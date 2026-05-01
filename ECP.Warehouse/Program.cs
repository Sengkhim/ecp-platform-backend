var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "Ready" }));
app.MapGet("/status", () =>  Results.Ok("The service warehouse is running...."));    
app.MapGet("/", () =>  Results.Json("The service warehouse is running....")); 

app.Run();