extern alias Cli;
using Cli::Portless.Cli.Services;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests.CliServices;

public class ProxyPortProviderTests
{
    [Fact]
    public void GetProxyPort_WithNoEnvVar_ReturnsDefaultPort()
    {
        // Clear env var to ensure default
        var original = Environment.GetEnvironmentVariable(ProxyConstants.PortEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ProxyConstants.PortEnvVar, null);
            var port = ProxyPortProvider.GetProxyPort();
            Assert.Equal(ProxyConstants.DefaultHttpPort, port);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ProxyConstants.PortEnvVar, original);
        }
    }

    [Fact]
    public void GetProxyPort_WithEnvVarOverride_ReturnsOverriddenPort()
    {
        var original = Environment.GetEnvironmentVariable(ProxyConstants.PortEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ProxyConstants.PortEnvVar, "8080");
            var port = ProxyPortProvider.GetProxyPort();
            Assert.Equal(8080, port);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ProxyConstants.PortEnvVar, original);
        }
    }

    [Fact]
    public void GetProxyPort_WithInvalidEnvVar_ReturnsDefaultPort()
    {
        var original = Environment.GetEnvironmentVariable(ProxyConstants.PortEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ProxyConstants.PortEnvVar, "not-a-number");
            var port = ProxyPortProvider.GetProxyPort();
            Assert.Equal(ProxyConstants.DefaultHttpPort, port);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ProxyConstants.PortEnvVar, original);
        }
    }

    [Fact]
    public void GetProxyPort_WithEmptyEnvVar_ReturnsDefaultPort()
    {
        var original = Environment.GetEnvironmentVariable(ProxyConstants.PortEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ProxyConstants.PortEnvVar, "");
            var port = ProxyPortProvider.GetProxyPort();
            Assert.Equal(ProxyConstants.DefaultHttpPort, port);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ProxyConstants.PortEnvVar, original);
        }
    }
}
