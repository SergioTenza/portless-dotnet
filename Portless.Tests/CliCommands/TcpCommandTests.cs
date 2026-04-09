extern alias Cli;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portless.Core.Serialization;
using TcpCommand = Cli::Portless.Cli.Commands.TcpCommand.TcpCommand;
using TcpSettings = Cli::Portless.Cli.Commands.TcpCommand.TcpSettings;
using Spectre.Console.Cli;
using Xunit;

namespace Portless.Tests.CliCommands;

[Collection("SpectreConsoleTests")]
public class TcpCommandTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly TcpCommand _command;

    public TcpCommandTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _command = new TcpCommand(_httpClientFactoryMock.Object, NullLogger<TcpCommand>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_Remove_NoName_Returns1()
    {
        // Arrange
        var settings = new TcpSettings { Remove = true, Name = null };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Remove_WithName_Success_Returns0()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(client);

        var settings = new TcpSettings { Remove = true, Name = "redis" };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Remove_HttpError_Returns1()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(throwOnSend: true);
        var client = new HttpClient(handler);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(client);

        var settings = new TcpSettings { Remove = true, Name = "redis" };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Add_NoName_Returns1()
    {
        // Arrange
        var settings = new TcpSettings { Remove = false, Name = null, Target = "localhost:6379", ListenPort = 6380 };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Add_NoTarget_Returns1()
    {
        // Arrange
        var settings = new TcpSettings { Remove = false, Name = "redis", Target = null, ListenPort = 6380 };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Add_NoListenPort_Returns1()
    {
        // Arrange
        var settings = new TcpSettings { Remove = false, Name = "redis", Target = "localhost:6379", ListenPort = null };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Add_InvalidTargetFormat_Returns1()
    {
        // Arrange
        var settings = new TcpSettings { Remove = false, Name = "redis", Target = "invalid-no-port", ListenPort = 6380 };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Add_Success_Returns0()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(client);

        var settings = new TcpSettings
        {
            Remove = false,
            Name = "redis",
            Target = "localhost:6379",
            ListenPort = 6380
        };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Add_ServerReturnsError_Returns1()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(statusCode: HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(client);

        var settings = new TcpSettings
        {
            Remove = false,
            Name = "redis",
            Target = "localhost:6379",
            ListenPort = 6380
        };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Add_HttpRequestException_Returns1()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(throwOnSend: true);
        var client = new HttpClient(handler);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(client);

        var settings = new TcpSettings
        {
            Remove = false,
            Name = "redis",
            Target = "localhost:6379",
            ListenPort = 6380
        };

        // Act
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }
}

/// <summary>
/// Mock HTTP handler for unit testing HttpClient-based commands.
/// </summary>
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly bool _throwOnSend;

    public MockHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, bool throwOnSend = false)
    {
        _statusCode = statusCode;
        _throwOnSend = throwOnSend;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_throwOnSend)
        {
            throw new HttpRequestException("Connection refused");
        }

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
