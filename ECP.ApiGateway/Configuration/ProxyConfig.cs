using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace ECP.ApiGateway.Configuration;

/// <summary>
/// A single immutable snapshot of routes + clusters.
/// When YARP calls GetConfig(), it subscribes to the ChangeToken.
/// Calling SignalChange() tells YARP to call GetConfig() again
/// and reload its internal router with the new snapshot.
/// </summary>
public sealed class ProxyConfig : IProxyConfig
{
    private readonly CancellationTokenSource _cts = new();

    public ProxyConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters)
    {
        Routes      = routes;
        Clusters    = clusters;
        ChangeToken = new CancellationChangeToken(_cts.Token);
    }

    public IReadOnlyList<RouteConfig>   Routes      { get; }
    public IReadOnlyList<ClusterConfig> Clusters    { get; }
    public IChangeToken                 ChangeToken { get; }

    /// <summary>
    /// Signal YARP that a new config snapshot is ready.
    /// YARP will call IProxyConfigProvider.GetConfig() again and
    /// rebuild its internal route table.
    /// </summary>
    public void SignalChange() => _cts.Cancel();
}