using System.Threading.RateLimiting;
using ECP.ApiGateway.Application.Configuration;
using ECP.ApiGateway.Application.Factory;
using ECP.ApiGateway.Application.Health;
using ECP.ApiGateway.Discovery;
using k8s;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Yarp.ReverseProxy.Configuration;

namespace ECP.ApiGateway.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddServiceConfigurationLayer(IConfiguration configuration)
        {
            services
                .AddHealthChecks()
                .AddCheck<KubernetesHealthCheck>("kubernetes", tags: ["ready"]);
        
            services.AddHttpClient();
            // services.AddOpenTelemetries(configuration);
            services.ConfigureRateLimiting(configuration);
            services.ConfigureResilience(configuration);     // ← new

            services.KubernetesConfiguration();
            services.ServiceDiscoveryConfigure();
            services.AddHostedService<YarpConfigVerifier>();
            
            var opts = services.GatewayOption(configuration);
            if (opts.EnableResponseCompression)
                services.AddResponseCompression();           // ← was missing
        }

        // private void AddOpenTelemetries()
        // {
        //     services.AddOpenTelemetry()
        //         .ConfigureResource(r => r.AddService(
        //             serviceName:    "api-gateway",
        //             serviceVersion: "1.0.0"))
        //         .WithTracing(tracing => tracing
        //             .AddAspNetCoreInstrumentation(o =>
        //             {
        //                 o.RecordException = true;
        //                 o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health")
        //                                   && !ctx.Request.Path.StartsWithSegments("/debug");
        //             })
        //             .AddHttpClientInstrumentation()
        //             .AddConsoleExporter());
        // }

        // private void AddOpenTelemetries(IConfiguration configuration)
        // {
        //     var otlpEndpoint = configuration["OpenTelemetry:Endpoint"]
        //                        ?? "http://localhost:4317";
        //
        //     services.AddOpenTelemetry()
        //         .ConfigureResource(r => r.AddService(
        //             serviceName:    "api-gateway",
        //             serviceVersion: "1.0.0"))
        //         .WithTracing(tracing => tracing
        //             .AddAspNetCoreInstrumentation(o =>
        //             {
        //                 o.RecordException = true;
        //                 o.Filter = ctx =>
        //                     !ctx.Request.Path.StartsWithSegments("/health") &&
        //                     !ctx.Request.Path.StartsWithSegments("/debug")  &&
        //                     !ctx.Request.Path.StartsWithSegments("/metrics");
        //             })
        //             .AddHttpClientInstrumentation()
        //             .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
        //         .WithMetrics(metrics => metrics
        //             .AddAspNetCoreInstrumentation()
        //             .AddHttpClientInstrumentation()
        //             .AddRuntimeInstrumentation()
        //             .AddMeter("ECP.ApiGateway")
        //             .AddPrometheusExporter());
        // }
        
        public GatewayOptions GatewayOption(IConfiguration configuration)
        {
            services.Configure<GatewayOptions>(configuration.GetSection(GatewayOptions.Section));

            var gatewayOptions = configuration
                .GetSection(GatewayOptions.Section)
                .Get<GatewayOptions>() ?? new GatewayOptions(); 
            
            return gatewayOptions;
        }
        
        private void ConfigureRateLimiting(IConfiguration configuration)
        {
            var gatewayOptions = services.GatewayOption(configuration);
            
            // ── Rate Limiting
            if (gatewayOptions.EnableRateLimiting)
            {
                services.AddRateLimiter(opts =>
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
        }

        private void KubernetesConfiguration()
        {
            // ── Kubernetes Client ──────────────────────────────────────────────────
            // KubernetesClientFactory handles all three environments automatically:
            //   - Production (in-cluster)   : uses service account token
            //   - Local dotnet run          : uses ~/.kube/config
            //   - Docker compose            : mounted kubeconfig + path/host patching
            services.AddSingleton<IKubernetes>(sp =>
                KubernetesClientFactory.Create(sp.GetRequiredService<ILogger<Program>>()));
        }

        private void ServiceDiscoveryConfigure()
        {
            var providers = services
                .Where(d => d.ServiceType == typeof(IProxyConfigProvider))
                .ToList();
            
            foreach (var d in providers)
                services.Remove(d);
            
            // ── YARP + Kubernetes Service Discovery 
            // IProxyConfigProvider MUST be registered BEFORE AddReverseProxy().
            // Never call .LoadFromMemory() — it registers a conflicting provider.
            services.AddSingleton<KubernetesServiceDiscoveryProvider>();
            services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<KubernetesServiceDiscoveryProvider>());
        }
        
        private void ConfigureResilience(IConfiguration configuration)
        {
            var opts = services.GatewayOption(configuration);

            services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler(resilience =>
                {
                    resilience.TotalRequestTimeout.Timeout =
                        TimeSpan.FromSeconds(opts.TimeoutSeconds);

                    if (!opts.CircuitBreakerEnabled)
                        resilience.CircuitBreaker.ShouldHandle =
                            new PredicateBuilder<HttpResponseMessage>()
                                .HandleResult(_ => false);  // disable: never trips
                });
            });
        }
    }
}