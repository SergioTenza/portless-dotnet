using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

/// <summary>
/// Custom HttpMessageHandler that allows controlling response behavior for tests.
/// </summary>
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handler(request);
    }
}

public class ProxyRouteRegistrarTests
{
    private readonly Mock<ILogger<ProxyRouteRegistrar>> _loggerMock;

    public ProxyRouteRegistrarTests()
    {
        _loggerMock = new Mock<ILogger<ProxyRouteRegistrar>>();
    }

    private ProxyRouteRegistrar CreateRegistrar(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler(handler));
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return new ProxyRouteRegistrar(factoryMock.Object, _loggerMock.Object);
    }

    private static Task<HttpResponseMessage> OkResponse()
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private static Task<HttpResponseMessage> ErrorResponse(HttpStatusCode statusCode, string content)
    {
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        });
    }

    [Fact]
    public async Task RegisterRouteAsync_SingleBackend_DelegatesToMultiBackendAndSucceeds()
    {
        // Arrange
        var registrar = CreateRegistrar(req =>
        {
            Assert.Equal("http://localhost:1355/api/v1/add-host", req.RequestUri?.ToString());
            return OkResponse();
        });

        // Act
        var result = await registrar.RegisterRouteAsync("test.localhost", "http://localhost:4042");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RegisterRouteAsync_MultiBackend_Succeeds()
    {
        // Arrange
        var registrar = CreateRegistrar(req =>
        {
            Assert.Equal("http://localhost:1355/api/v1/add-host", req.RequestUri?.ToString());
            return OkResponse();
        });

        // Act
        var backends = new[] { "http://localhost:4042", "http://localhost:4043" };
        var result = await registrar.RegisterRouteAsync("test.localhost", backends, loadBalancePolicy: "RoundRobin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RegisterRouteAsync_MultiBackend_FailsOnNonSuccessStatusCode()
    {
        // Arrange
        var registrar = CreateRegistrar(req => ErrorResponse(HttpStatusCode.InternalServerError, "Server error"));

        // Act
        var backends = new[] { "http://localhost:4042", "http://localhost:4043" };
        var result = await registrar.RegisterRouteAsync("test.localhost", backends);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RegisterRouteAsync_HandlesHttpRequestException()
    {
        // Arrange
        var registrar = CreateRegistrar(req => throw new HttpRequestException("Connection refused"));

        // Act
        var result = await registrar.RegisterRouteAsync("test.localhost", "http://localhost:4042");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveRouteAsync_Succeeds()
    {
        // Arrange
        var registrar = CreateRegistrar(req =>
        {
            Assert.Equal("http://localhost:1355/api/v1/remove-host", req.RequestUri?.ToString());
            return OkResponse();
        });

        // Act
        var result = await registrar.RemoveRouteAsync("test.localhost");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RemoveRouteAsync_FailsOnNonSuccessStatusCode()
    {
        // Arrange
        var registrar = CreateRegistrar(req => ErrorResponse(HttpStatusCode.NotFound, "Not found"));

        // Act
        var result = await registrar.RemoveRouteAsync("test.localhost");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveRouteAsync_HandlesHttpRequestException()
    {
        // Arrange
        var registrar = CreateRegistrar(req => throw new HttpRequestException("Connection refused"));

        // Act
        var result = await registrar.RemoveRouteAsync("test.localhost");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RegisterRouteAsync_SendsCorrectApiEndpointUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var registrar = CreateRegistrar(req =>
        {
            capturedRequest = req;
            return OkResponse();
        });

        // Act
        await registrar.RegisterRouteAsync("myapp.localhost", "http://localhost:5000");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("http://localhost:1355/api/v1/add-host", capturedRequest!.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("myapp.localhost", body);
        Assert.Contains("http://localhost:5000", body);
    }

    [Fact]
    public async Task RegisterRouteAsync_SingleBackend_SendsJsonPayloadWithPath()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var registrar = CreateRegistrar(req =>
        {
            capturedRequest = req;
            return OkResponse();
        });

        // Act
        await registrar.RegisterRouteAsync("api.localhost", "http://localhost:3000", "/api");

        // Assert
        Assert.NotNull(capturedRequest);
        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("api.localhost", body);
        Assert.Contains("http://localhost:3000", body);
        Assert.Contains("/api", body);
    }
}
