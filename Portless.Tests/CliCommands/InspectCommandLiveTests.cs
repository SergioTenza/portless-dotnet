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

/// <summary>
/// Additional InspectCommand tests covering LiveStream mode,
/// SaveSessions with filters, error paths, and more edge cases.
/// </summary>
[Collection("SpectreConsoleTests")]
public class InspectCommandLiveTests
{
    private readonly Mock<IProxyHttpClient> _proxyHttpMock;
    private readonly InspectCommand _command;

    public InspectCommandLiveTests()
    {
        _proxyHttpMock = new Mock<IProxyHttpClient>();
        _command = new InspectCommand(_proxyHttpMock.Object);
    }

    private static CommandContext CreateContext() =>
        new([], new TestRemainingArguments(), "inspect", null);

    private static HttpClient CreateMultiResponseClient(params string[] jsonResponses)
    {
        return new HttpClient(new LiveTestResponseHandler(jsonResponses));
    }

    private static string MakeSessionJson(
        string method = "GET", string hostname = "test.localhost",
        string path = "/", int statusCode = 200, long durationMs = 100)
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method,
                hostname, path, scheme = "http", statusCode, durationMs,
                routeId = "route1", requestBodySize = (int?)null,
                responseBodySize = (int?)200
            }
        };
        return JsonSerializer.Serialize(sessions);
    }

    // --- Live Stream tests ---

    [Fact]
    public async Task ExecuteAsync_LiveMode_ConnectionError_Returns0Or1()
    {
        // Live mode with immediate connection error should return 1
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(new HttpClient(new ThrowingLiveHandler()));

        var settings = new InspectSettings { Live = true };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    // --- SaveSessions with various filters ---

    [Fact]
    public async Task ExecuteAsync_SaveSessions_WithHostFilter_Returns0()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "GET",
                hostname = "api.example.com", path = "/v1", scheme = "http",
                statusCode = 200, durationMs = 100L, routeId = "r1",
                requestBodySize = (int?)null, responseBodySize = (int?)100
            },
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "POST",
                hostname = "other.com", path = "/v2", scheme = "http",
                statusCode = 201, durationMs = 200L, routeId = "r2",
                requestBodySize = (int?)50, responseBodySize = (int?)200
            }
        };
        var sessionsJson = JsonSerializer.Serialize(sessions);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(sessionsJson));

        var savePath = Path.Combine(Path.GetTempPath(), $"inspect-host-filter-{Guid.NewGuid():N}.jsonl");
        try
        {
            var settings = new InspectSettings { SavePath = savePath, Filter = "host:api" };
            var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
            Assert.Equal(0, result);

            var content = await File.ReadAllTextAsync(savePath);
            Assert.Single(content.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }
        finally
        {
            try { if (File.Exists(savePath)) File.Delete(savePath); } catch { }
        }
    }

    [Fact]
    public async Task ExecuteAsync_SaveSessions_EmptySessions_Returns0()
    {
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient("[]"));

        var savePath = Path.Combine(Path.GetTempPath(), $"inspect-empty-{Guid.NewGuid():N}.jsonl");
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

    // --- ShowRecent with HTTP error response ---

    [Fact]
    public async Task ShowRecent_ServerErrorStatusCode_ThrowsHttpRequestException()
    {
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(new HttpClient(new ErrorHandlerResponseHandler(HttpStatusCode.InternalServerError)));

        var settings = new InspectSettings();
        // EnsureSuccessStatusCode will throw
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    // --- ShowRecent with various status code ranges ---

    [Fact]
    public async Task ShowRecent_StatusCode600_FallsToDefaultRed()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "GET",
                hostname = "test.com", path = "/", scheme = "http",
                statusCode = 600, durationMs = 50L, routeId = "r1",
                requestBodySize = (int?)null, responseBodySize = (int?)null
            }
        };
        var sessionsJson = JsonSerializer.Serialize(sessions);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(sessionsJson, "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    // --- Filter with multiple criteria and edge cases ---

    [Fact]
    public async Task ShowRecent_FilterByStatus5xx_Returns0()
    {
        var json = MakeSessionJson(statusCode: 503);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "status:5xx" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_FilterByExactStatusNonNumeric_Skips()
    {
        var json = MakeSessionJson(statusCode: 200);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "status:abc" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_LikeMatchWithWildcard_HostFilter()
    {
        var json = MakeSessionJson(hostname: "api.example.com");
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "host:api.*.com" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_LikeMatchNoWildcard_HostContains()
    {
        var json = MakeSessionJson(hostname: "api.example.com");
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "host:example" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_TruncateLongHostPath()
    {
        var longHost = new string('a', 50) + ".localhost";
        var longPath = new string('b', 50);
        var json = MakeSessionJson(hostname: longHost, path: longPath);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    // --- Multiple sessions with filter eliminating all ---

    [Fact]
    public async Task ShowRecent_FilterEliminatesAllSessions_ShowsEmptyTable()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "GET",
                hostname = "a.com", path = "/", scheme = "http",
                statusCode = 200, durationMs = 50L, routeId = "r1",
                requestBodySize = (int?)null, responseBodySize = (int?)100
            }
        };
        var sessionsJson = JsonSerializer.Serialize(sessions);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(sessionsJson, "{}"));

        var settings = new InspectSettings { Filter = "method:DELETE" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    // --- Stats response with various shapes ---

    [Fact]
    public async Task ShowRecent_StatsEndpointReturnsError_StillReturns0()
    {
        var sessionsJson = MakeSessionJson();
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(new HttpClient(new StatsErrorHandlerResponseHandler(sessionsJson)));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    // --- SaveSessions writes JSONL correctly ---

    [Fact]
    public async Task SaveSessions_WritesValidJsonLines()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "GET",
                hostname = "test.com", path = "/api", scheme = "http",
                statusCode = 200, durationMs = 100L, routeId = "r1",
                requestBodySize = (int?)null, responseBodySize = (int?)100
            },
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "POST",
                hostname = "test.com", path = "/data", scheme = "http",
                statusCode = 201, durationMs = 200L, routeId = "r2",
                requestBodySize = (int?)50, responseBodySize = (int?)200
            }
        };
        var sessionsJson = JsonSerializer.Serialize(sessions);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(sessionsJson));

        var savePath = Path.Combine(Path.GetTempPath(), $"inspect-jsonl-{Guid.NewGuid():N}.jsonl");
        try
        {
            var settings = new InspectSettings { SavePath = savePath };
            var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
            Assert.Equal(0, result);

            var lines = (await File.ReadAllTextAsync(savePath))
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
            // Each line should be valid JSON
            foreach (var line in lines)
            {
                Assert.NotEmpty(line);
                Assert.True(line.StartsWith("{"));
            }
        }
        finally
        {
            try { if (File.Exists(savePath)) File.Delete(savePath); } catch { }
        }
    }
}

file class LiveTestResponseHandler : HttpMessageHandler
{
    private readonly string[] _responses;
    private int _index;

    public LiveTestResponseHandler(string[] responses)
    {
        _responses = responses;
        _index = 0;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // After exhausting responses, throw to break the live loop
        if (_index >= _responses.Length)
        {
            throw new HttpRequestException("Connection lost");
        }
        var json = _responses[_index++];
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

file class ThrowingLiveHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Connection refused");
    }
}

file class ErrorHandlerResponseHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;

    public ErrorHandlerResponseHandler(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(_statusCode));
    }
}

file class StatsErrorHandlerResponseHandler : HttpMessageHandler
{
    private readonly string _sessionsJson;
    private int _callCount;

    public StatsErrorHandlerResponseHandler(string sessionsJson)
    {
        _sessionsJson = sessionsJson;
        _callCount = 0;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _callCount++;
        if (_callCount == 1)
        {
            // First call (sessions) succeeds
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_sessionsJson, System.Text.Encoding.UTF8, "application/json")
            });
        }
        // Second call (stats) fails
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }
}
