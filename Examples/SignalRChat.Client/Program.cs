using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRChat.Client;

// SignalR Console Client Example
//
// This console application demonstrates how to connect to a SignalR hub
// and send/receive messages in real-time.
//
// Usage:
//   dotnet run
//   dotnet run -- http://chatsignalr.localhost:1355/chathub
//
// The client will:
// 1. Connect to the SignalR hub using WebSocket transport
// 2. Register a handler for receiving messages
// 3. Prompt for a username
// 4. Allow sending messages to all connected clients
// 5. Display messages received from other clients

class Program
{
    static async Task Main(string[] args)
    {
        // Get hub URL from command line or use default
        // Default assumes you're running the chat server locally
        var hubUrl = args.Length > 0
            ? args[0]
            : "http://localhost:5000/chathub";

        Console.WriteLine("SignalR Chat Console Client");
        Console.WriteLine("============================");
        Console.WriteLine($"Connecting to: {hubUrl}");
        Console.WriteLine();

        // Build the SignalR connection
        // HubConnectionBuilder creates a connection to the SignalR hub
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            // Automatic reconnect with default retry policy
            .WithAutomaticReconnect()
            .Build();

        // Register handler for receiving messages
        // This is called whenever the server broadcasts a "ReceiveMessage" event
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            // Display received message with timestamp
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine($"[{timestamp}] {user}: {message}");
        });

        // Handle connection events
        connection.Reconnecting += error =>
        {
            Console.WriteLine();
            Console.WriteLine($"Connection lost. Reconnecting... {(error?.Message ?? "Unknown error")}");
            return Task.CompletedTask;
        };

        connection.Reconnected += connectionId =>
        {
            Console.WriteLine();
            Console.WriteLine($"Reconnected to server (Connection ID: {connectionId})");
            return Task.CompletedTask;
        };

        connection.Closed += error =>
        {
            Console.WriteLine();
            Console.WriteLine($"Connection closed: {error?.Message ?? "Unknown reason"}");
            return Task.CompletedTask;
        };

        // Start the connection
        Console.Write("Connecting...");
        try
        {
            await connection.StartAsync();
            Console.WriteLine(" Connected!");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine(" Failed!");
            Console.WriteLine();
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Troubleshooting:");
            Console.WriteLine("1. Make sure the chat server is running");
            Console.WriteLine("2. Check the hub URL is correct");
            Console.WriteLine("3. Verify Portless.NET proxy is running (if using proxy)");
            Console.WriteLine("4. Check that the hostname is registered: portless list");
            return;
        }

        // Get username
        Console.Write("Enter your username: ");
        var username = Console.ReadLine()?.Trim() ?? "Anonymous";

        if (string.IsNullOrWhiteSpace(username))
        {
            username = "Anonymous";
        }

        Console.WriteLine();
        Console.WriteLine($"Connected as {username}");
        Console.WriteLine("Type messages and press Enter to send.");
        Console.WriteLine("Press Enter with empty line to quit.");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine();

        // Main message loop
        while (true)
        {
            // Read message from console
            var message = Console.ReadLine();

            // Empty line = quit
            if (string.IsNullOrWhiteSpace(message))
            {
                break;
            }

            // Send message to server
            try
            {
                await connection.InvokeAsync("SendMessage", username, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message: {ex.Message}");
            }
        }

        // Clean up
        Console.WriteLine();
        Console.Write("Disconnecting...");
        await connection.StopAsync();
        Console.WriteLine(" Done.");
        Console.WriteLine("Goodbye!");
    }
}
