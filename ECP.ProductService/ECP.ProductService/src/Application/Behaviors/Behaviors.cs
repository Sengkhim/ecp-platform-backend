using System.Diagnostics;
using ECP.ProductService.Application.Mappings;
using ECP.ProductService.Core.Domain.Entities;
using ECP.ProductService.Core.Exceptions;
using FluentValidation;
using MediatR;
using AppValidationException = ECP.ProductService.Core.Exceptions.AppValidationException;

namespace ECP.ProductService.Application.Behaviors;

/// <summary>
/// 1. Runs all FluentValidation validators before the handler.
/// 2. Aggregates all failures into a single AppValidationException.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next(ct);

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        if (failures.Count > 0)
        {
            logger.LogWarning("Validation failed for {Request}: {@Failures}", typeof(TRequest).Name, failures);
            throw new AppValidationException(failures);
        }

        return await next(ct);
    }
}

/// <summary>
/// Structured timing log for every request.
/// Emits a Warning if a request exceeds 500ms.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromMilliseconds(500);

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw   = Stopwatch.StartNew();

        try
        {
            var response = await next(ct);
            sw.Stop();

            if (sw.Elapsed > SlowThreshold)
                logger.LogWarning("[SLOW] {Request} completed in {Ms}ms", name, sw.ElapsedMilliseconds);
            else
                logger.LogInformation("{Request} completed in {Ms}ms", name, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "{Request} failed after {Ms}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
