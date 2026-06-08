namespace ECP.ApiGateway.Application.Configuration;

public sealed class GatewayOptions
{
    public const string Section = "Gateway";

    public string Namespace { get; init; } = string.Empty;
    
    public string LabelSelector { get; init; } = "gateway-exposed=true";
    
    public int DiscoveryIntervalSeconds { get; init; } = 30;
    public bool EnableRateLimiting { get; init; } = true;
    public int DefaultRateLimitPermitLimit { get; init; } = 100;
    public int DefaultRateLimitWindowSeconds { get; init; } = 60;
    
    // Features
    public bool   EnableResponseCompression      { get; init; } = true;
    public bool   EnableRequestLogging           { get; init; } = false;

    // Resilience  ← these were in config but not in the model
    public int    TimeoutSeconds                 { get; init; } = 30;
    public bool   CircuitBreakerEnabled          { get; init; } = true;
}