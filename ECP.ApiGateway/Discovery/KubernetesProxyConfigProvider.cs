// using ECP.ApiGateway.Configuration;
// using k8s.Models;
// using Yarp.ReverseProxy.Configuration;
// using Microsoft.Extensions.Options;
// using Microsoft.Extensions.Primitives;
// using Yarp.ReverseProxy.Health;
// using Yarp.ReverseProxy.LoadBalancing;
//
// namespace ECP.ApiGateway.Discovery;
//
// public sealed class KubernetesProxyConfigProvider(
//     IServiceDiscovery discovery,
//     IOptions<GatewayOptions> options,
//     ILogger<KubernetesProxyConfigProvider> logger)
//     : IProxyConfigProvider, IDisposable
// {
//     private volatile KubernetesProxyConfig _currentConfig = new([], []);
//     private readonly CancellationTokenSource _cts = new();
//     private Task? _refreshTask;
//
//     public IProxyConfig GetConfig() => _currentConfig;
//
//     public void StartBackgroundRefresh()
//     {
//         _refreshTask = RefreshLoop(_cts.Token);
//     }
//
//     private async Task RefreshLoop(CancellationToken cancellationToken)
//     {
//         while (!cancellationToken.IsCancellationRequested)
//         {
//             await RefreshAsync(cancellationToken);
//             await Task.Delay(
//                 TimeSpan.FromSeconds(options.Value.DiscoveryIntervalSeconds),
//                 cancellationToken);
//         }
//     }
//
//     public async Task RefreshAsync(CancellationToken cancellationToken = default)
//     {
//         var services = await discovery.GetServicesAsync(
//             options.Value.Namespace,
//             options.Value.LabelSelector,
//             cancellationToken);
//
//         var (routes, clusters) = BuildConfiguration(services);
//
//         var oldConfig = _currentConfig;
//         _currentConfig = new KubernetesProxyConfig(routes, clusters);
//         oldConfig.SignalChange();
//
//         logger.LogInformation(
//             "YARP config updated: {RouteCount} routes, {ClusterCount} clusters",
//             routes.Count, clusters.Count);
//     }
//
//     private static (List<RouteConfig> Routes, List<ClusterConfig> Clusters)
//         BuildConfiguration(IReadOnlyList<V1Service> services)
//     {
//         var routes = new List<RouteConfig>();
//         var clusters = new List<ClusterConfig>();
//
//         foreach (var svc in services)
//         {
//             var name = svc.Metadata.Name;
//             var annotations = svc.Metadata.Annotations ?? new Dictionary<string, string>();
//
//             // Convention: annotation "gateway/path-prefix" overrides default
//             var pathPrefix = annotations.TryGetValue("gateway/path-prefix", out var prefix)
//                 ? prefix
//                 : $"/{name}";
//
//             var port = svc.Spec.Ports?.FirstOrDefault()?.Port ?? 80;
//             var destinationAddress =
//                 $"http://{name}.{svc.Metadata.NamespaceProperty}:{port}";
//
//             // Strip path prefix when forwarding (configurable via annotation)
//             var stripPrefix = !annotations.TryGetValue("gateway/strip-prefix", out var strip)
//                 || bool.Parse(strip);
//
//             var routeTransforms = new List<IReadOnlyDictionary<string, string>>();
//             if (stripPrefix)
//             {
//                 routeTransforms.Add(new Dictionary<string, string>
//                 {
//                     ["PathRemovePrefix"] = pathPrefix
//                 });
//             }
//
//             routes.Add(new RouteConfig
//             {
//                 RouteId = $"route-{name}",
//                 ClusterId = $"cluster-{name}",
//                 Match = new RouteMatch
//                 {
//                     Path = $"{pathPrefix}/{{**catch-all}}"
//                 },
//                 Transforms = routeTransforms
//             });
//
//             clusters.Add(new ClusterConfig
//             {
//                 ClusterId = $"cluster-{name}",
//                 LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
//                 HealthCheck = new HealthCheckConfig
//                 {
//                     Passive = new PassiveHealthCheckConfig
//                     {
//                         Enabled = true,
//                         Policy = HealthCheckConstants.PassivePolicy.TransportFailureRate
//                     },
//                     Active = new ActiveHealthCheckConfig
//                     {
//                         Enabled = true,
//                         Interval = TimeSpan.FromSeconds(10),
//                         Timeout = TimeSpan.FromSeconds(5),
//                         Policy = HealthCheckConstants.ActivePolicy.ConsecutiveFailures,
//                         Path = annotations.TryGetValue("gateway/health-path", out var hp)
//                             ? hp : "/health"
//                     }
//                 },
//                 Destinations = new Dictionary<string, DestinationConfig>
//                 {
//                     [$"destination-{name}"] = new DestinationConfig
//                     {
//                         Address = destinationAddress
//                     }
//                 },
//                 HttpClient = new HttpClientConfig
//                 {
//                     MaxConnectionsPerServer = 256
//                 }
//             });
//         }
//
//         return (routes, clusters);
//     }
//
//     public void Dispose()
//     {
//         _cts.Cancel();
//         _cts.Dispose();
//     }
//
//     private sealed class KubernetesProxyConfig(
//         IReadOnlyList<RouteConfig> routes,
//         IReadOnlyList<ClusterConfig> clusters)
//         : IProxyConfig
//     {
//         private readonly CancellationTokenSource _cts = new();
//
//         public IReadOnlyList<RouteConfig> Routes { get; } = routes;
//         public IReadOnlyList<ClusterConfig> Clusters { get; } = clusters;
//         public IChangeToken ChangeToken { get; } =
//             new CancellationChangeToken(new CancellationTokenSource().Token);
//
//         // Called when a new config replaces this one
//         public void SignalChange() => _cts.Cancel();
//     }
// }