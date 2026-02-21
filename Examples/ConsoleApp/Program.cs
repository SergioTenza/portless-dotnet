var port = Environment.GetEnvironmentVariable("PORT");
Console.WriteLine("Portless Console App Example");
if (!string.IsNullOrEmpty(port))
{
    Console.WriteLine($"Running on port: {port} (assigned by Portless)");
    Console.WriteLine($"URL: http://localhost:{port}");
}
else
{
    Console.WriteLine("No PORT assigned - run with 'portless myconsole dotnet run'");
}
