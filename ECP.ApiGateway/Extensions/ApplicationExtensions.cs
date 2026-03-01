using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ECP.ApiGateway.Extensions;

public static class ApplicationExtensions
{
    extension(WebApplication app)
    {
        public void UseMapReverseProxy()
        {
            // ── YARP Reverse Proxy 
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
        }

        public void UseMapHealthChecks()
        {
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
        }
    }
}