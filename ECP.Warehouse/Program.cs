var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "Ready" }));
app.MapGet("/status", () =>  Results.Ok("The service warehouse is running...."));    
app.MapGet("/", () =>  Results.Json("The service warehouse is running....")); 
app.MapGet("/new", () =>  Results.Json("new")); 

app.MapGet("/env", () =>
{
    var hostProperties = builder.Host.Properties
        .Select(x => new
        {
            key = x.Key.ToString(),
            value = x.Value
        });

    return Results.Json(new
    {
        env = builder.Environment.EnvironmentName,
        service = builder.Environment.ApplicationName,
        root = builder.Environment.ContentRootPath,
        hostProperties
    });
});

app.Run();

// docker build -t epc.warehouse:v4 -f ECP.Warehouse/Dockerfile .
// docker tag epc.warehouse:v4 devkhim/epc.warehouse:v4  
// docker push devkhim/epc.warehouse:v4 