// using ECP.ApiGateway.Discovery;
// using Microsoft.Extensions.Diagnostics.HealthChecks;
//
// namespace ECP.ApiGateway.Health;
//
// public sealed class GatewayHealthCheck(
//     KubernetesProxyConfigProvider configProvider) : IHealthCheck
// {
//     public Task<HealthCheckResult> CheckHealthAsync(
//         HealthCheckContext context,
//         CancellationToken cancellationToken = default)
//     {
//         var config = configProvider.GetConfig();
//         var data = new Dictionary<string, object>
//         {
//             ["routes"] = config.Routes.Count,
//             ["clusters"] = config.Clusters.Count
//         };
//
//         return Task.FromResult(config.Routes.Count > 0
//             ? HealthCheckResult.Healthy("Gateway is routing traffic", data)
//             : HealthCheckResult.Degraded("No routes configured", data: data));
//     }
// }
//
// // <summary>