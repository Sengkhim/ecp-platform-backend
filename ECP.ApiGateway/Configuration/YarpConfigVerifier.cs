using Yarp.ReverseProxy.Configuration;

namespace ECP.ApiGateway.Configuration;

/// <summary>
/// Runs at startup and logs exactly which IProxyConfigProvider(s) YARP sees.
///
/// YARP internally resolves IEnumerable<IProxyConfigProvider> — if more than
/// one is registered, only the LAST one wins. This verifier shows you exactly
/// what's in the DI container so you can catch conflicts immediately.
/// </summary>
public sealed class YarpConfigVerifier(
    IEnumerable<IProxyConfigProvider> allProviders,
    ILogger<YarpConfigVerifier> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var list = allProviders.ToList();

        logger.LogInformation(
            "[YARP VERIFY] {Count} IProxyConfigProvider(s) in DI:", list.Count);

        for (var i = 0; i < list.Count; i++)
        {
            var config = list[i].GetConfig();
            logger.LogInformation(
                "[YARP VERIFY]   [{Index}] {Type} → {Routes} route(s), {Clusters} cluster(s)",
                i, list[i].GetType().Name,
                config.Routes.Count,
                config.Clusters.Count);

            foreach (var route in config.Routes)
                logger.LogInformation(
                    "[YARP VERIFY]       Route: {RouteId} | Path: {Path} | Transforms: {Transforms}",
                    route.RouteId,
                    route.Match.Path,
                    route.Transforms?.Count ?? 0);

            foreach (var cluster in config.Clusters)
                foreach (var (id, dest) in cluster.Destinations ?? new Dictionary<string, DestinationConfig>())
                    logger.LogInformation(
                        "[YARP VERIFY]       Cluster: {ClusterId} | {DestId} → {Address}",
                        cluster.ClusterId, id, dest.Address);
        }

        switch (list.Count)
        {
            case 0:
                logger.LogError("[YARP VERIFY] NO IProxyConfigProvider registered! YARP will return 404 for all requests.");
                break;
            case > 1:
                logger.LogWarning(
                    "[YARP VERIFY] Multiple providers found — YARP uses the LAST one. " +
                    "Remove LoadFromMemory() and any duplicate registrations.");
                break;
        }

        var active = list.LastOrDefault();
        if (active is null) return Task.CompletedTask;
        var routes = active.GetConfig().Routes.Count;
        if (routes == 0)
            logger.LogWarning(
                "[YARP VERIFY] Active provider has 0 routes. " +
                "K8s watcher may still be connecting — wait a few seconds and check /debug/routes.");
        else
            logger.LogInformation(
                "[YARP VERIFY] OK — active provider '{Type}' has {Routes} route(s)",
                active.GetType().Name, routes);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}