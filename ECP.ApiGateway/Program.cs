using System.Text.Json;
using Serilog;
using ECP.ApiGateway.Extensions;
using ECP.ApiGateway.Middleware;

// Bootstrap logger (before host builds) 
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Logging.AddJsonConsole(o =>
        o.JsonWriterOptions = new JsonWriterOptions { Indented = false });
    
    // ── Serilog 
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .WriteTo.Console());

    builder.Services
        .AddReverseProxy()
        .ConfigureHttpClient((_, handler) =>
        {
            handler.AllowAutoRedirect = false;
            handler.MaxConnectionsPerServer = 100;
        });
    
    builder.Services.AddServiceConfigurationLayer(builder.Configuration);
    var option = builder.Services.GatewayOption(builder.Configuration);
    
    var app = builder.Build();
    
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseRouting();
    
    // ── Observability
    // app.MapPrometheusScrapingEndpoint("/metrics");

    if (option.EnableResponseCompression)
        app.UseResponseCompression();
    
    if (option.EnableRateLimiting)
        app.UseRateLimiter();
    
    if (option.EnableRequestLogging)
        app.UseSerilogRequestLogging();
    
    app.UseMapHealthChecks();
    app.UseMapReverseProxy();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}