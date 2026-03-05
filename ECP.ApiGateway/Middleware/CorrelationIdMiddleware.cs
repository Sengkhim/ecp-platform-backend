namespace ECP.ApiGateway.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(Header, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            context.Request.Headers[Header] = correlationId;
        }

        context.Response.Headers[Header] = correlationId;

        using var scope = context.RequestServices
            .GetRequiredService<ILogger<CorrelationIdMiddleware>>()
            .BeginScope(new Dictionary<string, object> { [Header] = correlationId.ToString() });

        await next(context);
    }
}