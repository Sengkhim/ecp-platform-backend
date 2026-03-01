using k8s;

namespace ECP.ApiGateway.Application.Factory;

// <summary>
/// Builds the correct KubernetesClientConfiguration for the current environment.
///
/// Environment detection (in priority order):
///
///   1. In-cluster (Production K8s)
///      Detected by: KUBERNETES_SERVICE_HOST env var + mounted service account token
///      Uses:        InClusterConfig() — reads token from /var/run/secrets/...
///      Requires:    serviceAccountName + automountServiceAccountToken: true in deployment.yaml
///
///   2. Out-of-cluster (dotnet run locally)
///      Detected by: no service account token
///      Uses:        ~/.kube/config or KUBECONFIG env var
///      No extra config needed — kubectl context must point to minikube
///
///   3. Out-of-cluster inside Docker (docker compose)
///      Detected by: no service account token
///      Uses:        kubeconfig mounted via volume (-v ~/.kube:/home/app/.kube)
///      Extra:       Patches cert paths + API server URL because:
///                   - kubeconfig has Mac absolute paths (/Users/admin/.minikube/ca.crt)
///                     that don't exist inside the container
///                   - kubeconfig API server points to 127.0.0.1 which is the
///                     container itself, not the Mac
///
///      Required env vars in docker-compose.yml:
///        KUBECONFIG:              /home/app/.kube/config
///        MINIKUBE_HOST_PATH:      /Users/${USER}        ← Mac home prefix to replace
///        MINIKUBE_CONTAINER_PATH: /home/app             ← container mount prefix
///        K8S_API_SERVER:          https://host.minikube.internal:PORT
/// </summary>
public static class KubernetesClientFactory
{
    private const string ServiceAccountTokenPath =
        "/var/run/secrets/kubernetes.io/serviceaccount/token";

    public static IKubernetes Create(ILogger logger)=> new Kubernetes(BuildConfig(logger));
    
    private static KubernetesClientConfiguration BuildConfig(ILogger logger)
    {
        // In-cluster
        var k8SServiceHost = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST");
        
        if (!string.IsNullOrEmpty(k8SServiceHost) && File.Exists(ServiceAccountTokenPath))
        {
            logger.LogInformation("[K8s] Mode: in-cluster | API: {Host}", k8SServiceHost);
            return KubernetesClientConfiguration.InClusterConfig();
        }

        // Out-of-cluster (local dotnet run OR docker compose)
        var kubeConfigPath = Environment.GetEnvironmentVariable("KUBECONFIG")
                          ?? KubernetesClientConfiguration.KubeConfigDefaultLocation;

        logger.LogInformation("[K8s] Mode: out-of-cluster | kubeconfig: {Path}", kubeConfigPath);

        kubeConfigPath = PatchKubeConfig(kubeConfigPath, logger);

        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigPath);

        // Override API server URL — needed when running in Docker because
        // kubeconfig points to 127.0.0.1 which is the container, not the Mac.
        var apiServerOverride = Environment.GetEnvironmentVariable("K8S_API_SERVER");
        if (string.IsNullOrEmpty(apiServerOverride)) return config;
        
        logger.LogInformation(
            "[K8s] API server: {Old} → {New}", config.Host, apiServerOverride);
        
        config.Host = apiServerOverride;

        return config;
    }

    /// <summary>
    /// When running inside Docker, kubeconfig cert paths are Mac absolute paths
    /// (e.g. /Users/admin/.minikube/ca.crt) that don't exist in the container.
    /// Rewrites them to the container mount path and returns a temp file path.
    /// </summary>
    private static string PatchKubeConfig(string kubeConfigPath, ILogger logger)
    {
        var hostPrefix      = Environment.GetEnvironmentVariable("MINIKUBE_HOST_PATH");
        var containerPrefix = Environment.GetEnvironmentVariable("MINIKUBE_CONTAINER_PATH");

        if (string.IsNullOrEmpty(hostPrefix) || string.IsNullOrEmpty(containerPrefix))
            return kubeConfigPath;

        var raw = File.ReadAllText(kubeConfigPath);
        
        if (!raw.Contains(hostPrefix)) return kubeConfigPath;

        var patched  = raw.Replace(hostPrefix, containerPrefix);
        var tempPath = Path.Combine(Path.GetTempPath(), "kubeconfig-patched");
        File.WriteAllText(tempPath, patched);

        logger.LogInformation(
            "[K8s] Cert paths remapped: {From} → {To}", hostPrefix, containerPrefix);

        return tempPath;
    }
}