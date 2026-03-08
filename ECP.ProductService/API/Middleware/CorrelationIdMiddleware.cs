namespace ECP.ProductService.API.Middleware;

/// <summary>
/// Ensures every request has a correlation ID.
/// Reads X-Correlation-Id from inbound headers or generates a new one.
/// Adds it to the response and to the logging scope so all logs within
/// the request carry the same ID.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        ctx.Response.Headers[HeaderName] = correlationId;
        ctx.Items["CorrelationId"] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(ctx);
        }
    }
}
