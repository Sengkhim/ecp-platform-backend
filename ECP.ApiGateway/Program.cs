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
            handler.AllowAutoRedirect       = false;
            handler.MaxConnectionsPerServer = 100;
        });
    
    builder.Services.AddServiceConfigurationLayer(builder.Configuration);
    var option = builder.Services.GatewayOption(builder.Configuration);
    
    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseRouting();

    if (option.EnableRateLimiting)
        app.UseRateLimiter();

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