using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromMemory([],[]);

var app = builder.Build();

app.MapReverseProxy();

app.MapPost("/api/v1/add-host", (UpdateConfigRequest request) =>
{
    var config = app.Services.GetRequiredService<InMemoryConfigProvider>();
    
    config.Update(request.Routes, request.Clusters);
    
    return Results.Ok("Configuración actualizada correctamente.");
});

app.Run();


public record UpdateConfigRequest(
    IReadOnlyList<RouteConfig> Routes,  
    IReadOnlyList<ClusterConfig> Clusters
);