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
/// Additional InspectCommand tests focusing on filter edge cases,
/// status code color logic, and various response scenarios.
/// </summary>
[Collection("SpectreConsoleTests")]
public class InspectCommandExtendedTests
{
    private readonly Mock<IProxyHttpClient> _proxyHttpMock;
    private readonly InspectCommand _command;

    public InspectCommandExtendedTests()
    {
        _proxyHttpMock = new Mock<IProxyHttpClient>();
        _command = new InspectCommand(_proxyHttpMock.Object);
    }

    private static CommandContext CreateContext() =>
        new([], new TestRemainingArguments(), "inspect", null);

    private static HttpClient CreateMultiResponseClient(params string[] jsonResponses)
    {
        var handler = new SequentialResponseHandler(jsonResponses);
        return new HttpClient(handler);
    }

    private static HttpClient CreateThrowingClient()
    {
        return new HttpClient(new ThrowingHandler());
    }

    private static string MakeSessionJson(
        string method = "GET", string hostname = "test.localhost",
        string path = "/", int statusCode = 200, long durationMs = 100,
        string scheme = "http")
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(),
                timestamp = DateTime.Now,
                method,
                hostname,
                path,
                scheme,
                statusCode,
                durationMs,
                routeId = "route1",
                requestBodySize = (int?)null,
                responseBodySize = (int?)200
            }
        };
        return JsonSerializer.Serialize(sessions);
    }

    // --- Status code color coverage tests ---

    [Fact]
    public async Task ShowRecent_2xxStatus_Returns0()
    {
        var json = MakeSessionJson(statusCode: 200);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_3xxStatus_Returns0()
    {
        var json = MakeSessionJson(statusCode: 301);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_4xxStatus_Returns0()
    {
        var json = MakeSessionJson(statusCode: 404);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_5xxStatus_Returns0()
    {
        var json = MakeSessionJson(statusCode: 500);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_1xxStatus_Returns0()
    {
        // 100 is < 200, so it falls into "red" default case
        var json = MakeSessionJson(statusCode: 100);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    // --- Filter edge cases ---

    [Fact]
    public async Task ShowRecent_FilterByHost_Returns0()
    {
        var json = MakeSessionJson(hostname: "api.example.com");
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "host:api.example.com" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_FilterByPath_Returns0()
    {
        var json = MakeSessionJson(path: "/api/v1/users");
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "path:/api" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_FilterByStatus2xx_Returns0()
    {
        var json = MakeSessionJson(statusCode: 200);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "status:2xx" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_FilterByStatus3xx_Returns0()
    {
        var json = MakeSessionJson(statusCode: 301);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "status:3xx" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_FilterByStatus4xx_Returns0()
    {
        var json = MakeSessionJson(statusCode: 404);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "status:4xx" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_FilterByStatusCodeExact_Returns0()
    {
        var json = MakeSessionJson(statusCode: 404);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "status:404" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_FilterByMethodCaseInsensitive_Returns0()
    {
        var json = MakeSessionJson(method: "POST");
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "method:post" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_FilterWithWildcardHost_Returns0()
    {
        var json = MakeSessionJson(hostname: "api.example.com");
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "host:api.*.com" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_MultipleSessionsWithMixedStatus_Returns0()
    {
        var sessions = new[]
        {
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "GET",
                hostname = "a.localhost", path = "/", scheme = "http",
                statusCode = 200, durationMs = 50L, routeId = "r1",
                requestBodySize = (int?)null, responseBodySize = (int?)100
            },
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "POST",
                hostname = "b.localhost", path = "/api", scheme = "http",
                statusCode = 301, durationMs = 60L, routeId = "r2",
                requestBodySize = (int?)50, responseBodySize = (int?)200
            },
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "DELETE",
                hostname = "c.localhost", path = "/item", scheme = "http",
                statusCode = 404, durationMs = 70L, routeId = "r3",
                requestBodySize = (int?)null, responseBodySize = (int?)300
            },
            new
            {
                id = Guid.NewGuid(), timestamp = DateTime.Now, method = "PUT",
                hostname = "d.localhost", path = "/update", scheme = "http",
                statusCode = 500, durationMs = 80L, routeId = "r4",
                requestBodySize = (int?)10, responseBodySize = (int?)50
            }
        };
        var sessionsJson = JsonSerializer.Serialize(sessions);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(sessionsJson, "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_NullSessionsResponse_Returns0()
    {
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient("null", "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_StatsIsNull_Returns0()
    {
        var json = MakeSessionJson();
        // Stats response is null deserialization result
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "null"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_LongHostname_TruncatesAndReturns0()
    {
        var longHost = "this-is-a-very-long-hostname-that-should-be-truncated-in-the-table.localhost";
        var json = MakeSessionJson(hostname: longHost, path: "/very/long/path/that/exceeds/limit");
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings();
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    // --- Save sessions tests ---

    [Fact]
    public async Task SaveSessions_ConnectionFailure_Returns1()
    {
        _proxyHttpMock.Setup(x => x.CreateClient()).Returns(CreateThrowingClient());

        var settings = new InspectSettings { SavePath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.jsonl") };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task SaveSessions_WithFilter_Returns0()
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
                hostname = "other.example.com", path = "/v2", scheme = "http",
                statusCode = 201, durationMs = 200L, routeId = "r2",
                requestBodySize = (int?)50, responseBodySize = (int?)200
            }
        };
        var sessionsJson = JsonSerializer.Serialize(sessions);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(sessionsJson));

        var savePath = Path.Combine(Path.GetTempPath(), $"inspect-filtered-{Guid.NewGuid():N}.jsonl");
        try
        {
            var settings = new InspectSettings { SavePath = savePath, Filter = "method:GET" };
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
    public async Task SaveSessions_NullResponse_Returns0()
    {
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient("null"));

        var savePath = Path.Combine(Path.GetTempPath(), $"inspect-null-{Guid.NewGuid():N}.jsonl");
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
    public async Task ShowRecent_CommaSeparatedFilters_Returns0()
    {
        var json = MakeSessionJson(method: "POST", hostname: "api.test.com", statusCode: 200);
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "method:POST,status:2xx" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_InvalidFilterKey_DoesNotFilter_Returns0()
    {
        var json = MakeSessionJson();
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "unknown:value" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_FilterNoColon_SkipsFilter_Returns0()
    {
        var json = MakeSessionJson();
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "nocolon" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_EmptyFilter_Returns0()
    {
        var json = MakeSessionJson();
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Filter = "" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ShowRecent_CustomCount_Returns0()
    {
        var json = MakeSessionJson();
        _proxyHttpMock.Setup(x => x.CreateClient())
            .Returns(CreateMultiResponseClient(json, "{}"));

        var settings = new InspectSettings { Count = 10 };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);
        Assert.Equal(0, result);
    }
}

file class SequentialResponseHandler : HttpMessageHandler
{
    private readonly string[] _responses;
    private int _index;

    public SequentialResponseHandler(string[] responses)
    {
        _responses = responses;
        _index = 0;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var json = _responses[Math.Min(_index, _responses.Length - 1)];
        _index++;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

file class ThrowingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Connection refused");
    }
}
