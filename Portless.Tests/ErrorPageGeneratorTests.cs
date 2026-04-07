using Portless.Proxy.ErrorPages;
using Xunit;

namespace Portless.Tests;

public class ErrorPageGeneratorTests
{
    [Fact]
    public void Generate404_ContainsHostname()
    {
        var html = ErrorPageGenerator.Generate404("myapp.localhost", new[] { "api.localhost", "web.localhost" });
        Assert.Contains("myapp.localhost", html);
        Assert.Contains("404", html);
        Assert.Contains("api.localhost", html);
        Assert.Contains("web.localhost", html);
    }

    [Fact]
    public void Generate404_WithEmptyRoutes_NoCrash()
    {
        var html = ErrorPageGenerator.Generate404("test.localhost", []);
        Assert.Contains("404", html);
        Assert.DoesNotContain("Active Routes", html);
    }

    [Fact]
    public void Generate502_ContainsHostname()
    {
        var html = ErrorPageGenerator.Generate502("api.localhost", 4001, null);
        Assert.Contains("api.localhost", html);
        Assert.Contains("502", html);
        Assert.Contains("4001", html);
    }

    [Fact]
    public void Generate502_WithReason_ShowsContext()
    {
        var html = ErrorPageGenerator.Generate502("api.localhost", null, "process_dead");
        Assert.Contains("terminated", html);
    }

    [Fact]
    public void Generate508_ContainsLoopInfo()
    {
        var html = ErrorPageGenerator.Generate508("api.localhost", 5);
        Assert.Contains("508", html);
        Assert.Contains("5 hops", html);
        Assert.Contains("Loop Detected", html);
    }

    [Fact]
    public void AllErrorPages_AreValidHtml()
    {
        var pages = new[]
        {
            ErrorPageGenerator.Generate404("test.localhost", []),
            ErrorPageGenerator.Generate502("test.localhost", 4001, null),
            ErrorPageGenerator.Generate508("test.localhost", 3),
        };

        foreach (var html in pages)
        {
            Assert.Contains("<!DOCTYPE html>", html);
            Assert.Contains("</html>", html);
            Assert.Contains("<style>", html);
        }
    }
}
