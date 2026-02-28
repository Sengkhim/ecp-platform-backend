using k8s;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;
using System.Text;
using ECP.ApiGateway.Configuration;
using ECP.ApiGateway.Discovery;
using ECP.ApiGateway.Health;
using ECP.ApiGateway.Middleware;
using HealthChecks.UI.Client;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

// ── Bootstrap logger (before host builds) ─────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .WriteTo.Console());

    // ── Options ────────────────────────────────────────────────────────────
    builder.Services.Configure<GatewayOptions>(
        builder.Configuration.GetSection(GatewayOptions.Section));

    var gatewayOptions = builder.Configuration
        .GetSection(GatewayOptions.Section)
        .Get<GatewayOptions>() ?? new GatewayOptions();

    // ── Kubernetes Client ──────────────────────────────────────────────────
    builder.Services.AddSingleton<IKubernetes>(_ =>
    {
        var config = KubernetesClientConfiguration.IsInCluster()
            ? KubernetesClientConfiguration.InClusterConfig()
            : KubernetesClientConfiguration.BuildConfigFromConfigFile();
        return new Kubernetes(config);
    });

    // ── YARP + Kubernetes Service Discovery ────────────────────────────────
    // IProxyConfigProvider MUST be registered BEFORE AddReverseProxy().
    // Never call .LoadFromMemory() — it registers a conflicting provider.
    builder.Services.AddSingleton<KubernetesServiceDiscoveryProvider>();
    builder.Services.AddSingleton<IProxyConfigProvider>(sp =>
        sp.GetRequiredService<KubernetesServiceDiscoveryProvider>());

    builder.Services
        .AddReverseProxy()
        .ConfigureHttpClient((context, handler) =>
        {
            handler.AllowAutoRedirect       = false;
            handler.MaxConnectionsPerServer = 100;
        });

    // Logs at startup which provider YARP resolved + how many routes loaded
    builder.Services.AddHostedService<YarpConfigVerifier>();

    // ── Rate Limiting ──────────────────────────────────────────────────────
    if (gatewayOptions.EnableRateLimiting)
    {
        builder.Services.AddRateLimiter(opts =>
        {
            opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = gatewayOptions.DefaultRateLimitPermitLimit,
                        Window               = TimeSpan.FromSeconds(gatewayOptions.DefaultRateLimitWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 10
                    }));

            opts.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded.", ct);
            };
        });
    }

    // ── OpenTelemetry ──────────────────────────────────────────────────────
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(
            serviceName:    "api-gateway",
            serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(o =>
            {
                o.RecordException = true;
                o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health")
                               && !ctx.Request.Path.StartsWithSegments("/debug");
            })
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());

    // ── Health Checks ──────────────────────────────────────────────────────
    builder.Services
        .AddHealthChecks()
        .AddCheck<KubernetesHealthCheck>("kubernetes", tags: ["ready"]);

    // ── HttpClient (for /debug/probe and /debug/forward) ───────────────────
    builder.Services.AddHttpClient();

    // ══════════════════════════════════════════════════════════════════════
    // BUILD
    // ══════════════════════════════════════════════════════════════════════
    var app = builder.Build();

    // ── Middleware Pipeline ────────────────────────────────────────────────
    // ORDER MATTERS — do not rearrange.
    //
    //  1. UseSerilogRequestLogging
    //  2. UseMiddleware<CorrelationIdMiddleware>
    //  3. UseRouting        ← CRITICAL: must come before MapReverseProxy
    //  4. UseRateLimiter    ← must come after UseRouting
    //  5. Map* endpoints    ← health + debug + YARP proxy

    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();

    // CRITICAL: Without UseRouting() YARP never matches any routes.
    // Requests fall through the entire pipeline and return 404.
    app.UseRouting();

    if (gatewayOptions.EnableRateLimiting)
        app.UseRateLimiter();

    // ── Health Endpoints ───────────────────────────────────────────────────

    // Liveness — is the process alive (no dependency checks)
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate      = _ => false,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Readiness — can we reach Kubernetes
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate      = hc => hc.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Combined
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // ── Debug Endpoints (Development only) ────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        // GET /debug/routes
        // Shows all YARP routes, destinations and transforms currently loaded.
        app.MapGet("/debug/routes", (IProxyConfigProvider configProvider) =>
        {
            var config = configProvider.GetConfig();
            var sb     = new StringBuilder();

            sb.AppendLine($"=== YARP Routes ({config.Routes.Count}) ===");

            foreach (var route in config.Routes)
            {
                var cluster = config.Clusters.FirstOrDefault(c => c.ClusterId == route.ClusterId);

                sb.AppendLine();
                sb.AppendLine($"  Route    : {route.RouteId}");
                sb.AppendLine($"  Match    : {route.Match.Path}");
                sb.AppendLine($"  Cluster  : {route.ClusterId}");

                if (cluster is not null)
                {
                    foreach (var (id, dest) in cluster.Destinations ?? new Dictionary<string, DestinationConfig>())
                        sb.AppendLine($"  Dest [{id}]: {dest.Address}");

                    sb.AppendLine($"  Timeout  : {cluster.HttpRequest?.ActivityTimeout}");
                    sb.AppendLine($"  LB Policy: {cluster.LoadBalancingPolicy}");
                }

                if (route.Transforms is { Count: > 0 })
                {
                    sb.AppendLine("  Transforms:");
                    foreach (var t in route.Transforms)
                        sb.AppendLine($"    {string.Join(" | ", t.Select(kv => $"{kv.Key}={kv.Value}"))}");
                }

                if (route.Metadata is { Count: > 0 })
                {
                    sb.AppendLine("  Metadata:");
                    foreach (var (k, v) in route.Metadata)
                        sb.AppendLine($"    {k} = {v}");
                }
            }

            if (config.Routes.Count == 0)
                sb.AppendLine("  No routes registered. " +
                              "Check namespace, proxy.gateway/enabled=true annotation, " +
                              "and that the K8s watcher has connected.");

            return Results.Text(sb.ToString());
        });

        // GET /debug/probe?url=http://analystservice.local/status&host=analystservice.local
        // Raw HTTP probe — bypasses YARP entirely.
        app.MapGet("/debug/probe", async (string url, string? host, IHttpClientFactory factory) =>
        {
            try
            {
                var client  = factory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (!string.IsNullOrEmpty(host))
                    request.Headers.Host = host;

                var response = await client.SendAsync(request);
                var body     = await response.Content.ReadAsStringAsync();

                return Results.Text(
                    $"URL    : {url}\n" +
                    $"Host   : {request.Headers.Host ?? "(not set)"}\n" +
                    $"Status : {(int)response.StatusCode} {response.StatusCode}\n" +
                    $"Body   : {body}");
            }
            catch (Exception ex)
            {
                return Results.Text($"ERROR: {ex.GetType().Name}: {ex.Message}");
            }
        });

        // GET /debug/forward/{**path}
        // Manually forwards to analystservice.local with correct Host header.
        // If this works but /api/analyst/** does not → YARP transform issue.
        app.MapGet("/debug/forward/{**path}", async (string path, IHttpClientFactory factory) =>
        {
            var client  = factory.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Get, $"http://analystservice.local/{path}");

            request.Headers.Host = "analystservice.local";

            var response = await client.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();

            return Results.Text(
                $"Forwarded : http://analystservice.local/{path}\n" +
                $"Host      : analystservice.local\n" +
                $"Status    : {(int)response.StatusCode} {response.StatusCode}\n" +
                $"Body      : {body}");
        });
    }

    // ── YARP Reverse Proxy ─────────────────────────────────────────────────
    app.MapReverseProxy(pipeline =>
    {
        // Diagnostic middleware — logs the exact request YARP forwards
        pipeline.Use(async (context, next) =>
        {
            var logger       = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var proxyFeature = context.GetReverseProxyFeature();
            var dest         = proxyFeature.AvailableDestinations
                                   .FirstOrDefault()?.Model.Config.Address ?? "unknown";

            logger.LogInformation(
                "[YARP PRE]  Route={Route} | Path={Path} | Host={Host} | Dest={Dest} | Transforms={Count}",
                proxyFeature.Route.Config.RouteId,
                context.Request.Path,
                context.Request.Headers.Host.ToString(),
                dest,
                proxyFeature.Route.Config.Transforms?.Count ?? 0);

            if (proxyFeature.Route.Config.Transforms is { } transforms)
                foreach (var t in transforms)
                    logger.LogInformation("[YARP TRANSFORM] {T}",
                        string.Join(" | ", t.Select(kv => $"{kv.Key}={kv.Value}")));

            await next();

            logger.LogInformation("[YARP POST] Status={Status}", context.Response.StatusCode);
        });

        pipeline.UseSessionAffinity();
        pipeline.UseLoadBalancing();
        pipeline.UsePassiveHealthChecks();
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}