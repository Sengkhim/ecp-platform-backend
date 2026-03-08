using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ECP.ProductService.API.Health;

public static class HealthCheckExtensions
{
    public static IEndpointRouteBuilder MapServiceHealthChecks(
        this IEndpointRouteBuilder app)
    {
        // /health — overall liveness for Kubernetes liveness probe
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
        });

        // /health/ready — readiness probe (only infra checks)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = hc => hc.Tags.Contains("db") || hc.Tags.Contains("cache"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        // /health/live — liveness probe (always 200 if process is running)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
        });

        return app;
    }
}
