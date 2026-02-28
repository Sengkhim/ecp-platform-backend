namespace ECP.ApiGateway.Configuration;

public sealed class GatewayOptions
{
    public const string Section = "Gateway";

    public string Namespace { get; init; } = "weather-api";
    public string LabelSelector { get; init; } = "gateway-exposed=true";
    public int DiscoveryIntervalSeconds { get; init; } = 30;
    public bool EnableRateLimiting { get; init; } = true;
    public int DefaultRateLimitPermitLimit { get; init; } = 100;
    public int DefaultRateLimitWindowSeconds { get; init; } = 60;
}