using ECP.ApiGateway.Middleware;
using ECP.ProductService.API.Extensions; 
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System",    LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ECP.ProductService");

    var builder = WebApplication.CreateBuilder(args);
    
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services
        .AddMongoDB(builder.Configuration)
        .AddRedisCache(builder.Configuration)
        .AddApplicationLayer()
        .AddGraphQLServices(builder.Environment)
        .AddServiceHealthChecks(builder.Configuration);

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
        app.UseDeveloperExceptionPage();

    app.MapGraphQL("/graphql");

    if (app.Environment.IsDevelopment())
        app.MapNitroApp("/graphql/ui");
    
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    // /health/ready → checks MongoDB + Redis (readiness probe)
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate      = hc => hc.Tags.Contains("db") || hc.Tags.Contains("cache"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // /health → all checks combined
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate      = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ECP.ProductService terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}