using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace ECP.ApiGateway.Discovery;

/// <summary>
/// Thread-safe snapshot of YARP routes + clusters.
/// Each rebuild creates a new instance; the old one signals its change token.
/// </summary>
public sealed class InMemoryConfigProvider : IProxyConfigProvider
{
    private readonly InMemoryConfig _config;

    public InMemoryConfigProvider(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters)
    {
        _config = new InMemoryConfig(routes, clusters);
    }

    public IProxyConfig GetConfig() => _config;

    // ── Inner snapshot ─────────────────────────────────────────────────────

    private sealed class InMemoryConfig : IProxyConfig
    {
        private readonly CancellationTokenSource _cts = new();

        public InMemoryConfig(
            IReadOnlyList<RouteConfig> routes,
            IReadOnlyList<ClusterConfig> clusters)
        {
            Routes   = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        public IReadOnlyList<RouteConfig>   Routes   { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; }
        public IChangeToken                  ChangeToken { get; }

        /// <summary>Signal watchers that a new config is available.</summary>
        public void SignalChange() => _cts.Cancel();
    }
}