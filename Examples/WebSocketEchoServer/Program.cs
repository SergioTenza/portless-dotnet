using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "WebSocket Echo Server. Connect to /ws to test WebSocket functionality.");

app.MapGet("/health", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    websocketEndpoint = "/ws"
});

// WebSocket echo endpoint
app.Map("/ws", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var buffer = new byte[1024 * 4]; // 4KB buffer

        logger.LogInformation("WebSocket connection established from {RemoteEndPoint}",
            context.Connection.RemoteIpAddress);

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("WebSocket close request received from {RemoteEndPoint}",
                        context.Connection.RemoteIpAddress);
                    await webSocket.CloseAsync(
                        receiveResult.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        receiveResult.CloseStatusDescription ?? "Closing",
                        CancellationToken.None);
                    break;
                }

                // Echo the message back
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                logger.LogDebug("Received message: {Message}", receivedMessage);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                    receiveResult.MessageType,
                    receiveResult.EndOfMessage,
                    CancellationToken.None);

                logger.LogDebug("Echoed message back to client");
            }

            logger.LogInformation("WebSocket connection closed from {RemoteEndPoint}",
                context.Connection.RemoteIpAddress);
        }
        catch (WebSocketException ex)
        {
            logger.LogError(ex, "WebSocket error from {RemoteEndPoint}",
                context.Connection.RemoteIpAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in WebSocket handler");
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("This endpoint accepts only WebSocket requests.");
    }
});

app.Run();
