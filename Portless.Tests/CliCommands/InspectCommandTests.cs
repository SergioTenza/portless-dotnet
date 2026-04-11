extern alias Cli;
using System.Net;
using System.Text.Json;
using Cli::Portless.Cli.Commands.InspectCommand;
using Cli::Portless.Cli.Services;
using Moq;
using Spectre.Console.Cli;
using Xunit;

using IProxyHttpClient = Cli::Portless.Cli.Services.IProxyHttpClient;

namespace Portless.Tests.CliCommands;

[Collection("SpectreConsoleTests")]
public class InspectCommandTests
{
    private readonly Mock<IProxyHttpClient> _proxyHttpMock;
    private readonly InspectCommand _command;

    public InspectCommandTests()
    {
        _proxyHttpMock = new Mock<IProxyHttpClient>();
        _command = new InspectCommand(_proxyHttpMock.Object);
    }

    private static CommandContext CreateContext() =>
        new([], new TestRemainingArguments(), "inspect", null);

    private static HttpClient CreateMockClient(string json)
    {
        var handler = new FakeHttpMessageHandler(json);
        return new HttpClient(handler);
    }

    private static HttpClient CreateMockClientWithTwoResponses(string json1, string json2)
    {
        var handler = new MultiResponseHttpMessageHandler(json1, json2);
        return new HttpClient(handler);
    }

    private static HttpClient CreateMockClientThatThrows()
    {
        var handler = new ThrowingHttpMessageHandler();
        return new HttpClient(handler);
    }

    [Fact]
    public async Task ExecuteAsync_ShowRecent_NoSessions_Returns0()
    {
        // Two calls: first for sessions, second for stats
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMockClientWithTwoResponses("[]", "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowRecent_WithSessions_Returns0()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(),
                timestamp = DateTime.Now,
                method = "GET",
                hostname = "example.com",
                path = "/api/test",
                scheme = "https",
                statusCode = 200,
                durationMs = 150L,
                routeId = "route1",
                requestBodySize = (int?)null,
                responseBodySize = (int?)1024
            }
        };
        var stats = new { totalCaptured = 10, avgDurationMs = 120.5, errorRate = 0.05 };
        var sessionsJson = JsonSerializer.Serialize(sessions);
        var statsJson = JsonSerializer.Serialize(stats);

        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMockClientWithTwoResponses(sessionsJson, statsJson));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowRecent_ConnectionFailure_Returns1()
    {
        _proxyHttpMock.Setup(x => x.CreateClient()).Returns(CreateMockClientThatThrows());

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_SaveSessions_NoSessions_Returns0()
    {
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMockClient("[]"));

        var savePath = Path.GetTempFileName();
        try
        {
            var settings = new InspectSettings { SavePath = savePath };
            var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
            Assert.Equal(0, result);
        }
        finally
        {
            try { if (File.Exists(savePath)) File.Delete(savePath); } catch { }
        }
    }

    [Fact]
    public async Task ExecuteAsync_SaveSessions_WithSessions_Returns0()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(),
                timestamp = DateTime.Now,
                method = "POST",
                hostname = "api.test.com",
                path = "/data",
                scheme = "https",
                statusCode = 201,
                durationMs = 200L,
                routeId = "route2",
                requestBodySize = (int?)50,
                responseBodySize = (int?)200
            }
        };
        var sessionsJson = JsonSerializer.Serialize(sessions);

        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMockClient(sessionsJson));

        var savePath = Path.Combine(Path.GetTempPath(), $"inspect-save-{Guid.NewGuid():N}.jsonl");
        try
        {
            var settings = new InspectSettings { SavePath = savePath };
            var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
            Assert.Equal(0, result);

            Assert.True(File.Exists(savePath));
            var content = await File.ReadAllTextAsync(savePath);
            Assert.NotEmpty(content);
        }
        finally
        {
            try { if (File.Exists(savePath)) File.Delete(savePath); } catch { }
        }
    }

    [Fact]
    public async Task ExecuteAsync_SaveSessions_ConnectionFailure_Returns1()
    {
        _proxyHttpMock.Setup(x => x.CreateClient()).Returns(CreateMockClientThatThrows());

        var settings = new InspectSettings { SavePath = "/tmp/test.jsonl" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowRecent_WithFilter_Returns0()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(),
                timestamp = DateTime.Now,
                method = "GET",
                hostname = "api.example.com",
                path = "/v1/users",
                scheme = "https",
                statusCode = 200,
                durationMs = 100L,
                routeId = "route1",
                requestBodySize = (int?)null,
                responseBodySize = (int?)500
            },
            new
            {
                id = Guid.NewGuid(),
                timestamp = DateTime.Now,
                method = "POST",
                hostname = "other.example.com",
                path = "/v1/data",
                scheme = "https",
                statusCode = 500,
                durationMs = 300L,
                routeId = "route2",
                requestBodySize = (int?)100,
                responseBodySize = (int?)50
            }
        };
        var sessionsJson = JsonSerializer.Serialize(sessions);

        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMockClientWithTwoResponses(sessionsJson, "{}"));

        var settings = new InspectSettings { Filter = "method:GET" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowRecent_StatusFilter_Returns0()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(),
                timestamp = DateTime.Now,
                method = "GET",
                hostname = "example.com",
                path = "/",
                scheme = "https",
                statusCode = 500,
                durationMs = 50L,
                routeId = "r1",
                requestBodySize = (int?)null,
                responseBodySize = (int?)null
            }
        };
        var sessionsJson = JsonSerializer.Serialize(sessions);

        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMockClientWithTwoResponses(sessionsJson, "{}"));

        var settings = new InspectSettings { Filter = "status:5xx" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowRecent_WithStats_Returns0()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(),
                timestamp = DateTime.Now,
                method = "GET",
                hostname = "test.com",
                path = "/api",
                scheme = "https",
                statusCode = 200,
                durationMs = 80L,
                routeId = "r1",
                requestBodySize = (int?)null,
                responseBodySize = (int?)100
            }
        };
        var stats = new { totalCaptured = 5, avgDurationMs = 95.0, errorRate = 0.1 };
        var sessionsJson = JsonSerializer.Serialize(sessions);
        var statsJson = JsonSerializer.Serialize(stats);

        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMockClientWithTwoResponses(sessionsJson, statsJson));

        var settings = new InspectSettings { Count = 10 };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowRecent_InvalidJson_ThrowsJsonException()
    {
        // Empty string cannot be deserialized as JSON
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMockClient(""));

        var settings = new InspectSettings();
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            () => _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None));
    }
}

file class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _json;

    public FakeHttpMessageHandler(string json)
    {
        _json = json;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

file class MultiResponseHttpMessageHandler : HttpMessageHandler
{
    private readonly string _json1;
    private readonly string _json2;
    private int _callCount;

    public MultiResponseHttpMessageHandler(string json1, string json2)
    {
        _json1 = json1;
        _json2 = json2;
        _callCount = 0;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var json = _callCount++ == 0 ? _json1 : _json2;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

file class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Connection refused");
    }
}
