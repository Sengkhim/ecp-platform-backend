using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ECP.ApiGateway.Extensions;

public static class ApplicationExtensions
{
    extension(WebApplication app)
    {
        public void UseMapReverseProxy()
        {
            app.MapReverseProxy(pipeline =>
            {
                pipeline.Use(async (context, next) =>
                {
                    var logger = context.RequestServices
                        .GetRequiredService<ILogger<Program>>();

                    var proxyFeature = context.GetReverseProxyFeature();
                    var dest = proxyFeature.AvailableDestinations
                        .FirstOrDefault()?.Model.Config.Address ?? "unknown";
                    
                    logger.LogInformation(
                        "[YARP PRE]  Route={Route} | IncomingPath={Path} | IncomingHost={Host}",
                        proxyFeature.Route.Config.RouteId,
                        context.Request.Path,
                        context.Request.Headers.Host.ToString());

                    logger.LogInformation(
                        "[YARP PRE]  Destination={Dest} | Transforms={Count}",
                        dest,
                        proxyFeature.Route.Config.Transforms?.Count ?? 0);
                    
                    if (proxyFeature.Route.Config.Transforms is { } trs)
                        foreach (var t in trs)
                            logger.LogInformation("[YARP TRANSFORM] {T}",
                                string.Join(" | ", t.Select(kv => $"{kv.Key}={kv.Value}")));

                    await next();

                    logger.LogInformation(
                        "[YARP POST] Status={Status} | FinalPath={Path} | FinalHost={Host}",
                        context.Response.StatusCode,
                        context.Request.Path,
                        context.Request.Headers.Host.ToString());
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