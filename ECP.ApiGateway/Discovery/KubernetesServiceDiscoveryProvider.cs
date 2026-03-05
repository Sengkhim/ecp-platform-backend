using k8s;
using k8s.Models;
using Yarp.ReverseProxy.Configuration;
using System.Collections.Concurrent;
using ECP.ApiGateway.Application.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace ECP.ApiGateway.Discovery;

/// <summary>
/// Watches Kubernetes Ingress resources and dynamically builds
/// YARP RouteConfig + ClusterConfig from annotations.
///
/// How YARP config reload works:
///   1. YARP calls GetConfig() on startup → gets the current ProxyConfig snapshot
///   2. YARP subscribes to ProxyConfig.ChangeToken
///   3. K8s watcher fires → RebuildConfig() creates a NEW ProxyConfig snapshot
///   4. OLD snapshot.SignalChange() is called → ChangeToken fires
///   5. YARP detects the token fire → calls GetConfig() again → gets new snapshot
///   6. YARP rebuilds its internal router — routes are now live
///
/// Required ingress annotations:
///   proxy.gateway/enabled:    "true"
///   proxy.gateway/route-path: "/api/analyst/{**catch-all}"
///
/// Optional ingress annotations:
///   proxy.gateway/strip-prefix:    "true"
///   proxy.gateway/destination-url: "http://analystservice.local"
///   proxy.gateway/host-header:     "analystservice.local"
///   proxy.gateway/scheme:          "http" | "https"
///   proxy.gateway/health-path:     "/health"
///
/// Auto-read nginx annotations:
///   nginx.ingress.kubernetes.io/proxy-connect-timeout
///   nginx.ingress.kubernetes.io/proxy-read-timeout
///   nginx.ingress.kubernetes.io/proxy-send-timeout
///   nginx.ingress.kubernetes.io/proxy-body-size
///   nginx.ingress.kubernetes.io/enable-cors
///   nginx.ingress.kubernetes.io/cors-allow-origin
///   nginx.ingress.kubernetes.io/cors-allow-methods
///   nginx.ingress.kubernetes.io/cors-allow-headers
/// </summary>
public sealed class KubernetesServiceDiscoveryProvider : IProxyConfigProvider, IDisposable
{
    // ── proxy.gateway/* ────────────────────────────────────────────────────
    private const string GwEnabled        = "proxy.gateway/enabled";
    private const string GwRoutePath      = "proxy.gateway/route-path";
    private const string GwDestinationUrl = "proxy.gateway/destination-url";
    private const string GwHostHeader     = "proxy.gateway/host-header";
    private const string GwStripPrefix    = "proxy.gateway/strip-prefix";
    private const string GwScheme         = "proxy.gateway/scheme";
    private const string GwHealthPath     = "proxy.gateway/health-path";

    // ── nginx.ingress.kubernetes.io/* ──────────────────────────────────────
    private const string NginxConnectTimeout = "nginx.ingress.kubernetes.io/proxy-connect-timeout";
    private const string NginxReadTimeout    = "nginx.ingress.kubernetes.io/proxy-read-timeout";
    private const string NginxSendTimeout    = "nginx.ingress.kubernetes.io/proxy-send-timeout";
    private const string NginxBodySize       = "nginx.ingress.kubernetes.io/proxy-body-size";
    private const string NginxEnableCors     = "nginx.ingress.kubernetes.io/enable-cors";
    private const string NginxCorsOrigin     = "nginx.ingress.kubernetes.io/cors-allow-origin";
    private const string NginxCorsMethods    = "nginx.ingress.kubernetes.io/cors-allow-methods";
    private const string NginxCorsHeaders    = "nginx.ingress.kubernetes.io/cors-allow-headers";

    private readonly IKubernetes _k8S;
    private readonly ILogger<KubernetesServiceDiscoveryProvider> _logger;
    private readonly string _namespace;
    private readonly string _ingressBaseUrl;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, V1Ingress> _cache = new();

    // Current snapshot — replaced atomically on every rebuild
    private ProxyConfig _current;

    [Obsolete("Obsolete")]
    public KubernetesServiceDiscoveryProvider(
        IKubernetes k8S,
        ILogger<KubernetesServiceDiscoveryProvider> logger,
        IConfiguration configuration)
    {
        _k8S            = k8S;
        _logger         = logger;
        _namespace      = configuration["Kubernetes:Namespace"] ?? "weather-api";
        _ingressBaseUrl = configuration["Kubernetes:IngressBaseUrl"] ?? "http://analystservice.local";

        // Start with an empty snapshot — YARP subscribes to its ChangeToken.
        // The K8s watcher will fire immediately with existing ingresses,
        // triggering RebuildConfig which signals the token and loads real routes.
        _current = new ProxyConfig([], []);

        _ = WatchIngressAsync(_cts.Token);
    }

    /// <summary>
    /// Called by YARP on startup and on every ChangeToken fire.
    /// Returns the current immutable snapshot.
    /// </summary>
    public IProxyConfig GetConfig() => _current;

    #region Kubernetes Watcher
    [Obsolete("Obsolete")]
    private async Task WatchIngressAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    "Starting Kubernetes Ingress watcher — namespace: '{Namespace}'",
                    _namespace);

                var response = _k8S.NetworkingV1
                    .ListNamespacedIngressWithHttpMessagesAsync(
                        _namespace, watch: true, cancellationToken: ct);

                await foreach (var (type, ingress) in response
                                   .WatchAsync<V1Ingress, V1IngressList>(
                                       onError: ex => _logger.LogWarning(ex, "Ingress watch error"),
                                       cancellationToken: ct))
                {
                    var key = $"{ingress.Namespace()}/{ingress.Name()}";

                    switch (type)
                    {
                        case WatchEventType.Added:
                        case WatchEventType.Modified:
                            _cache[key] = ingress;
                            break;
                        case WatchEventType.Deleted:
                            _cache.TryRemove(key, out _);
                            break;
                        case WatchEventType.Error:
                        case WatchEventType.Bookmark:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    RebuildConfig();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ingress watcher crashed — restarting in 5 s");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }
    #endregion

    #region  Config Rebuild
    private void RebuildConfig()
    {
        var routes   = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var (key, ingress) in _cache)
        {
            var ann = ingress.Metadata.Annotations
                      ?? new Dictionary<string, string>();

            // ── required ───────────────────────────────────────────────────
            if (!ann.TryGetValue(GwEnabled, out var enabled)
                || !string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!ann.TryGetValue(GwRoutePath, out var routePath)
                || string.IsNullOrWhiteSpace(routePath))
            {
                _logger.LogWarning("Ingress {Key} missing '{Ann}'", key, GwRoutePath);
                continue;
            }

            // ── optional proxy.gateway/* ───────────────────────────────────
            ann.TryGetValue(GwDestinationUrl, out var destinationUrl);
            ann.TryGetValue(GwHostHeader,     out var hostHeaderOverride);

            ann.TryGetValue(GwStripPrefix, out var stripVal);
            var stripPrefix = string.Equals(stripVal, "true", StringComparison.OrdinalIgnoreCase);

            ann.TryGetValue(GwScheme, out var schemeRaw);
            var scheme = string.IsNullOrWhiteSpace(schemeRaw) ? "http" : schemeRaw.ToLower();

            ann.TryGetValue(GwHealthPath, out var healthPath);

            // ── nginx timeouts ─────────────────────────────────────────────
            ann.TryGetValue(NginxReadTimeout,    out var readRaw);
            ann.TryGetValue(NginxSendTimeout,    out var sendRaw);
            ann.TryGetValue(NginxConnectTimeout, out var connRaw);
            var activityTimeout = TimeSpan.FromSeconds(
                Math.Max(
                    int.TryParse(readRaw, out var r) ? r : 60,
                    int.TryParse(sendRaw, out var s) ? s : 60));
            var connectSecs = int.TryParse(connRaw, out var c) ? c : 30;

            // ── nginx body size ────────────────────────────────────────────
            ann.TryGetValue(NginxBodySize, out var bodySizeRaw);
            var maxBodyBytes = ParseBodySize(bodySizeRaw);

            // ── nginx CORS ─────────────────────────────────────────────────
            ann.TryGetValue(NginxEnableCors,  out var corsEnabledRaw);
            ann.TryGetValue(NginxCorsOrigin,  out var corsOrigin);
            ann.TryGetValue(NginxCorsMethods, out var corsMethods);
            ann.TryGetValue(NginxCorsHeaders, out var corsHeaders);
            var corsActive = string.Equals(
                corsEnabledRaw, "true", StringComparison.OrdinalIgnoreCase);

            // ── destination + host ─────────────────────────────────────────
            var destinationAddress = ResolveDestinationAddress(ingress, destinationUrl, scheme);
            var effectiveHost      = hostHeaderOverride
                                  ?? ExtractHostFromUrl(destinationUrl)
                                  ?? ExtractIngressRuleHost(ingress)
                                  ?? new Uri(_ingressBaseUrl).Host;

            var ingressName = ingress.Name();
            var clusterId   = $"cluster-{ingressName}";
            var routeId     = $"route-{ingressName}";

            // ── transforms ─────────────────────────────────────────────────
            var transforms = new List<IReadOnlyDictionary<string, string>>
            {
                // Stop YARP forwarding client Host (localhost:5028) to upstream
                new Dictionary<string, string>
                {
                    ["RequestHeaderOriginalHost"] = "false"
                },
                // Set the correct Host nginx expects (analystservice.local)
                new Dictionary<string, string>
                {
                    ["RequestHeader"] = "Host",
                    ["Set"]           = effectiveHost
                }
            };

            // Strip gateway prefix before forwarding: /api/analyst/status → /status
            if (stripPrefix)
            {
                var prefix = routePath.Split('{')[0].TrimEnd('/');
                if (!string.IsNullOrEmpty(prefix))
                    transforms.Add(new Dictionary<string, string>
                    {
                        ["PathRemovePrefix"] = prefix
                    });
            }

            // Mirror CORS response headers from nginx annotations
            if (corsActive)
            {
                if (!string.IsNullOrEmpty(corsOrigin))
                    transforms.Add(new Dictionary<string, string>
                    {
                        ["ResponseHeader"] = "Access-Control-Allow-Origin",
                        ["Append"]         = corsOrigin
                    });
                if (!string.IsNullOrEmpty(corsMethods))
                    transforms.Add(new Dictionary<string, string>
                    {
                        ["ResponseHeader"] = "Access-Control-Allow-Methods",
                        ["Append"]         = corsMethods
                    });
                if (!string.IsNullOrEmpty(corsHeaders))
                    transforms.Add(new Dictionary<string, string>
                    {
                        ["ResponseHeader"] = "Access-Control-Allow-Headers",
                        ["Append"]         = corsHeaders
                    });
            }

            routes.Add(new RouteConfig
            {
                RouteId    = routeId,
                ClusterId  = clusterId,
                Match      = new RouteMatch { Path = routePath },
                Transforms = transforms,
                Metadata   = new Dictionary<string, string>
                {
                    ["IngressName"]  = ingressName,
                    ["Namespace"]    = ingress.Namespace(),
                    ["CorsEnabled"]  = corsActive.ToString(),
                    ["MaxBodyBytes"] = maxBodyBytes?.ToString() ?? "unlimited"
                }
            });

            clusters.Add(new ClusterConfig
            {
                ClusterId    = clusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["dest-0"] = new DestinationConfig
                    {
                        Address  = destinationAddress,
                        Metadata = new Dictionary<string, string>
                        {
                            ["HostHeader"] = effectiveHost
                        }
                    }
                },
                HealthCheck = healthPath is not null
                    ? new HealthCheckConfig
                    {
                        Passive = new PassiveHealthCheckConfig
                        {
                            Enabled = true,
                            Policy  = "TransportFailureRate"
                        },
                        Active = new ActiveHealthCheckConfig
                        {
                            Enabled  = true,
                            Path     = healthPath,
                            Interval = TimeSpan.FromSeconds(15),
                            Timeout  = TimeSpan.FromSeconds(5),
                            Policy   = "ConsecutiveFailures"
                        }
                    }
                    : null,
                HttpRequest         = new ForwarderRequestConfig { ActivityTimeout = activityTimeout },
                LoadBalancingPolicy = "RoundRobin",
                Metadata            = new Dictionary<string, string>
                {
                    ["ConnectTimeout"] = connectSecs.ToString(),
                    ["MaxBodyBytes"]   = maxBodyBytes?.ToString() ?? "unlimited"
                }
            });

            _logger.LogInformation(
                "Registered route: {Route} → {Dest} (Host: {Host}) strip={Strip}",
                routePath, destinationAddress, effectiveHost, stripPrefix);
        }
        
        var newConfig = new ProxyConfig(routes, clusters);
        var oldConfig = Interlocked.Exchange(ref _current, newConfig);
        oldConfig.SignalChange();

        _logger.LogInformation(
            "YARP config rebuilt — {Count} gateway-enabled ingress(es)", routes.Count);
    }
    #endregion

    #region  Hepler method

    private string ResolveDestinationAddress(V1Ingress ingress, string? explicitUrl, string scheme)
    {
        if (!string.IsNullOrWhiteSpace(explicitUrl))
            return explicitUrl.TrimEnd('/');

        var host = ExtractIngressRuleHost(ingress);
        
        return !string.IsNullOrWhiteSpace(host)
            ? $"{scheme}://{host}" 
            : _ingressBaseUrl.TrimEnd('/');
    }

    private static string? ExtractHostFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
    }

    private static string? ExtractIngressRuleHost(V1Ingress ingress)
        => ingress.Spec?.Rules?.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Host))?.Host;

    private static long? ParseBodySize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "0") return null;
        raw = raw.Trim().ToLower();
        if (raw.EndsWith('g') && long.TryParse(raw[..^1], out var gb)) return gb * 1024 * 1024 * 1024;
        if (raw.EndsWith('m') && long.TryParse(raw[..^1], out var mb)) return mb * 1024 * 1024;
        if (raw.EndsWith('k') && long.TryParse(raw[..^1], out var kb)) return kb * 1024;
        if (long.TryParse(raw, out var b)) return b;
        return null;
    }
    
    #endregion

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}