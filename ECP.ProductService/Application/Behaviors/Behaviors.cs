using FluentValidation;
using MediatR;
using ValidationException = ECP.ProductService.Core.Exceptions.ValidationException;

namespace ECP.ProductService.Application.Behaviors;

/// <summary>
/// Validates every command/query before the handler runs.
/// Throws ValidationException if any rules fail — caught by GraphQL error filter.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)));
        var errors  = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        if (errors.Count > 0)
            throw new ValidationException(errors);

        return await next();
    }
}

/// <summary>
/// Logs every request with timing. Warns on slow operations.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromMilliseconds(500);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var sw          = System.Diagnostics.Stopwatch.StartNew();

        logger.LogDebug("Handling {Request}", requestName);

        try
        {
            var response = await next();
            sw.Stop();

            if (sw.Elapsed > SlowThreshold)
                logger.LogWarning("Slow request {Request} took {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            else
                logger.LogDebug("Handled {Request} in {Elapsed}ms", requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Request {Request} failed after {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}