using SignalRChat;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR services
builder.Services.AddSignalR();

var app = builder.Build();

// Enable default files and static files
app.UseDefaultFiles();
app.UseStaticFiles();

// Map the SignalR hub
app.MapHub<ChatHub>("/chathub");

// Get the port from environment variable (set by Portless.NET)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

app.Run($"http://0.0.0.0:{port}");
