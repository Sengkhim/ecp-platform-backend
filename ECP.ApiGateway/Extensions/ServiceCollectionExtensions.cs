using System.Threading.RateLimiting;
using ECP.ApiGateway.Configuration;
using ECP.ApiGateway.Discovery;
using ECP.ApiGateway.Health;
using k8s;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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
            services.AddOpenTelemetries();
            services.ConfigureRateLimiting(configuration);
            services.KubernetesConfiguration();
            services.ServiceDiscoveryConfigure();
            services.AddHostedService<YarpConfigVerifier>();
        }

        private void AddOpenTelemetries()
        {
            services.AddOpenTelemetry()
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
        }

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
            // ── Kubernetes Client 
            services.AddSingleton<IKubernetes>(_ =>
            {
                var config = KubernetesClientConfiguration.IsInCluster()
                    ? KubernetesClientConfiguration.InClusterConfig()
                    : KubernetesClientConfiguration.BuildConfigFromConfigFile();
                return new Kubernetes(config);
            });
        }

        private void ServiceDiscoveryConfigure()
        {
            // ── YARP + Kubernetes Service Discovery 
            // IProxyConfigProvider MUST be registered BEFORE AddReverseProxy().
            // Never call .LoadFromMemory() — it registers a conflicting provider.
            services.AddSingleton<KubernetesServiceDiscoveryProvider>();
            services.AddSingleton<IProxyConfigProvider>(sp =>
                sp.GetRequiredService<KubernetesServiceDiscoveryProvider>());
        }
    }
}