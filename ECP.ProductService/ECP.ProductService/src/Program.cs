using ECP.ProductService.API.Extensions;
using ECP.ProductService.API.Health;
using ECP.ProductService.API.Middleware;
using Serilog;
using Serilog.Events;

// ── Bootstrap logger (pre-host) ───────────────────────────────────────────────
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

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // ── Services ──────────────────────────────────────────────────────────────
    builder.Services
        .AddMongoDB(builder.Configuration)
        .AddRedisCache(builder.Configuration)
        .AddApplicationLayer()
        .AddGraphQL(builder.Environment)
        .AddServiceHealthChecks(builder.Configuration);

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // ── GraphQL endpoint ──────────────────────────────────────────────────────
    app.MapGraphQL("/graphql");

    // In development, serve the Banana Cake Pop IDE
    if (app.Environment.IsDevelopment())
        app.MapBananaCakePop("/graphql/ui");

    // ── Health endpoints ──────────────────────────────────────────────────────
    app.MapServiceHealthChecks();

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
