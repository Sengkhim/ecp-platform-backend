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


/// <summary>
/// Ensures every request carries a Correlation-Id header.
/// Generates one if not present and propagates it downstream + back to the caller.
/// </summary>
// public sealed class CorrelationIdMiddleware(RequestDelegate next)
// {
//     public const string HeaderName = "X-Correlation-Id";
//
//     public async Task InvokeAsync(HttpContext context)
//     {
//         if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId)
//             || string.IsNullOrWhiteSpace(correlationId))
//         {
//             correlationId = Guid.NewGuid().ToString("N");
//             context.Request.Headers[HeaderName] = correlationId;
//         }
//
//         context.Response.Headers[HeaderName] = correlationId;
//         context.Items[HeaderName] = correlationId.ToString();
//
//         await next(context);
//     }
// }