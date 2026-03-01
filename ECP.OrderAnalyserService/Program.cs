var builder = WebApplication.CreateBuilder(args);

// Add health check services
builder.Services.AddHealthChecks();

var app = builder.Build();

// Sample endpoint
app.MapGet("/start", () => Results.Ok("Service is running...."));

// Liveness check (container is alive)
app.MapHealthChecks("/health/live");

// Readiness check (ready to receive traffic)
app.MapHealthChecks("/health/ready");

app.Run();