using k8s;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECP.ApiGateway.Health;

/// <summary>
/// Verifies the API gateway can reach the Kubernetes API server
/// and list ingress resources in the configured namespace.
/// </summary>
public sealed class KubernetesHealthCheck(
    IKubernetes k8SClient,
    IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ns = configuration["Kubernetes:Namespace"] ?? "weather-api";

            var ingresses = await k8SClient.NetworkingV1.ListNamespacedIngressAsync(
                ns,
                limit: 10,
                cancellationToken: cancellationToken);

            var enabled = ingresses.Items
                .Count(i => i.Metadata.Annotations
                    ?.TryGetValue("proxy.gateway/enabled", out var v) == true && v == "true");

            return HealthCheckResult.Healthy(
                $"Kubernetes API reachable. Namespace '{ns}': " +
                $"{ingresses.Items.Count} ingress(es), {enabled} gateway-enabled.",
                data: new Dictionary<string, object>
                {
                    ["namespace"]        = ns,
                    ["totalIngresses"]   = ingresses.Items.Count,
                    ["enabledIngresses"] = enabled
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Cannot reach Kubernetes API server.", exception: ex);
        }
    }
}