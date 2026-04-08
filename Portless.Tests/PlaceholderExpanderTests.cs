using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class PlaceholderExpanderTests
{
    [Fact]
    public void Expand_ReplacesPort()
    {
        var result = PlaceholderExpander.Expand("http://localhost:{PORT}", 4001, "myapp.localhost");
        Assert.Equal("http://localhost:4001", result);
    }

    [Fact]
    public void Expand_ReplacesHost()
    {
        var result = PlaceholderExpander.Expand("--host {HOST}", 4001, "myapp.localhost");
        Assert.Equal("--host 127.0.0.1", result);
    }

    [Fact]
    public void Expand_ReplacesUrl()
    {
        var result = PlaceholderExpander.Expand("URL={URL}", 4001, "myapp.localhost");
        Assert.Equal("URL=http://myapp.localhost", result);
    }

    [Fact]
    public void Expand_ReplacesName()
    {
        var result = PlaceholderExpander.Expand("--name {NAME}", 4001, "myapp.localhost");
        Assert.Equal("--name myapp", result);
    }

    [Fact]
    public void Expand_ReplacesAllPlaceholders()
    {
        var result = PlaceholderExpander.Expand("{PORT} {HOST} {URL} {NAME}", 4001, "api.localhost");
        Assert.Equal("4001 127.0.0.1 http://api.localhost api", result);
    }

    [Fact]
    public void ExpandArgs_ExpandsAllArgs()
    {
        var args = new[] { "--port", "{PORT}", "--host", "{HOST}" };
        var result = PlaceholderExpander.ExpandArgs(args, 4001, "api.localhost");
        Assert.Equal(new[] { "--port", "4001", "--host", "127.0.0.1" }, result);
    }

    [Fact]
    public void ExpandEnvVars_ParsesKeyValuePairs()
    {
        var templates = new[] { "PORT={PORT}", "URL={URL}" };
        var result = PlaceholderExpander.ExpandEnvVars(templates, 4001, "api.localhost");
        Assert.Equal("4001", result["PORT"]);
        Assert.Equal("http://api.localhost", result["URL"]);
    }
}
