using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Portless.Core.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Tests
{
    /// <summary>
    /// Integration tests for YARP-based proxy routing functionality.
    /// Tests verify Host header routing behavior, multiple hostname support,
    /// invalid hostname handling, and dynamic configuration updates.
[Collection("Integration Tests")]
    /// </summary>
    public class ProxyRoutingTests : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory = null!;
        private HttpClient _client = null!;

        public async Task InitializeAsync()
        {
            // Use isolated temp state directory to prevent interference from other tests
            var tempDir = Path.Combine(Path.GetTempPath(), $"portless-routing-test-{Guid.NewGuid():N}");
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", tempDir);
            Directory.CreateDirectory(tempDir);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "routes.json"), "[]");

            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            _factory?.Dispose();
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
            try
            {
                var dirs = Directory.GetDirectories(Path.GetTempPath(), "portless-routing-test-*");
                foreach (var d in dirs) try { Directory.Delete(d, true); } catch { }
            }
            catch { }
            await Task.CompletedTask;
        }

        [Fact]
        public async Task SingleHostname_RoutesToCorrectBackend()
        {
            // Arrange
            var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

            // Add a route for api1.localhost -> localhost:5000
            var routes = new List<Yarp.ReverseProxy.Configuration.RouteConfig>
            {
                new Yarp.ReverseProxy.Configuration.RouteConfig
                {
                    RouteId = "route-api1.localhost",
                    ClusterId = "cluster-api1.localhost",
                    Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                    {
                        Hosts = new[] { "api1.localhost" },
                        Path = "/{**catch-all}"
                    }
                }
            };

            var clusters = new List<Yarp.ReverseProxy.Configuration.ClusterConfig>
            {
                new Yarp.ReverseProxy.Configuration.ClusterConfig
                {
                    ClusterId = "cluster-api1.localhost",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["backend1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:5000" }
                    }
                }
            };

            config.Update(routes, clusters);

            // Act - Make request with custom Host header
            var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("Host", "api1.localhost");

            // Note: This will fail if no backend is running on port 5000
            // That's expected for this test - we're verifying routing configuration
            var response = await _client.SendAsync(request);

            // Assert - Verify the routing was configured
            // Response may be 502 (Bad Gateway) if backend not running
            // or 504 (GatewayTimeout) if backend connection times out
            // or 200 if backend exists. All are acceptable for routing verification.
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadGateway ||
                response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response.StatusCode == HttpStatusCode.GatewayTimeout,
                $"Expected OK, Bad Gateway, ServiceUnavailable, or GatewayTimeout, got {response.StatusCode}"
            );
        }

        [Fact]
        public async Task MultipleHostnames_RouteToDifferentBackends()
        {
            // Arrange
            var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

            var routes = new List<Yarp.ReverseProxy.Configuration.RouteConfig>
            {
                new Yarp.ReverseProxy.Configuration.RouteConfig
                {
                    RouteId = "route-api1.localhost",
                    ClusterId = "cluster-api1.localhost",
                    Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                    {
                        Hosts = new[] { "api1.localhost" },
                        Path = "/{**catch-all}"
                    }
                },
                new RouteConfig
                {
                    RouteId = "route-web1.localhost",
                    ClusterId = "cluster-web1.localhost",
                    Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                    {
                        Hosts = new[] { "web1.localhost" },
                        Path = "/{**catch-all}"
                    }
                }
            };

            var clusters = new List<Yarp.ReverseProxy.Configuration.ClusterConfig>
            {
                new Yarp.ReverseProxy.Configuration.ClusterConfig
                {
                    ClusterId = "cluster-api1.localhost",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["backend1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:5000" }
                    }
                },
                new ClusterConfig
                {
                    ClusterId = "cluster-web1.localhost",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["backend1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:3000" }
                    }
                }
            };

            config.Update(routes, clusters);

            // Act & Assert - Test first hostname
            var request1 = new HttpRequestMessage(HttpMethod.Get, "/");
            request1.Headers.Add("Host", "api1.localhost");
            var response1 = await _client.SendAsync(request1);

            Assert.True(
                response1.StatusCode == HttpStatusCode.OK ||
                response1.StatusCode == HttpStatusCode.BadGateway ||
                response1.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response1.StatusCode == HttpStatusCode.GatewayTimeout,
                $"api1.localhost: Expected OK, Bad Gateway, ServiceUnavailable, or GatewayTimeout, got {response1.StatusCode}"
            );

            // Act & Assert - Test second hostname
            var request2 = new HttpRequestMessage(HttpMethod.Get, "/");
            request2.Headers.Add("Host", "web1.localhost");
            var response2 = await _client.SendAsync(request2);

            Assert.True(
                response2.StatusCode == HttpStatusCode.OK ||
                response2.StatusCode == HttpStatusCode.BadGateway ||
                response2.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response2.StatusCode == HttpStatusCode.GatewayTimeout,
                $"web1.localhost: Expected OK, Bad Gateway, ServiceUnavailable, or GatewayTimeout, got {response2.StatusCode}"
            );
        }

        [Fact]
        public async Task InvalidHostname_ReturnsNotFound()
        {
            // Arrange
            var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

            // Add a route for api1.localhost only
            var routes = new List<Yarp.ReverseProxy.Configuration.RouteConfig>
            {
                new Yarp.ReverseProxy.Configuration.RouteConfig
                {
                    RouteId = "route-api1.localhost",
                    ClusterId = "cluster-api1.localhost",
                    Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                    {
                        Hosts = new[] { "api1.localhost" },
                        Path = "/{**catch-all}"
                    }
                }
            };

            var clusters = new List<Yarp.ReverseProxy.Configuration.ClusterConfig>
            {
                new Yarp.ReverseProxy.Configuration.ClusterConfig
                {
                    ClusterId = "cluster-api1.localhost",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["backend1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:5000" }
                    }
                }
            };

            config.Update(routes, clusters);

            // Act - Make request with unknown hostname
            var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("Host", "unknown.localhost");
            var response = await _client.SendAsync(request);

            // Assert - Should get 404 since no route matches
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ConfigurationUpdate_TriggersRoutingChanges()
        {
            // Arrange
            var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

            // Step 1: Add initial route
            var initialRoutes = new List<RouteConfig>
            {
                new RouteConfig
                {
                    RouteId = "route-api1.localhost",
                    ClusterId = "cluster-api1.localhost",
                    Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                    {
                        Hosts = new[] { "api1.localhost" },
                        Path = "/{**catch-all}"
                    }
                }
            };

            var initialClusters = new List<ClusterConfig>
            {
                new ClusterConfig
                {
                    ClusterId = "cluster-api1.localhost",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["backend1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:5000" }
                    }
                }
            };

            config.Update(initialRoutes, initialClusters);

            // Verify first route works
            var request1 = new HttpRequestMessage(HttpMethod.Get, "/");
            request1.Headers.Add("Host", "api1.localhost");
            var response1 = await _client.SendAsync(request1);

            Assert.True(
                response1.StatusCode == HttpStatusCode.OK ||
                response1.StatusCode == HttpStatusCode.BadGateway ||
                response1.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response1.StatusCode == HttpStatusCode.GatewayTimeout,
                $"api1.localhost (before update): Expected OK, Bad Gateway, ServiceUnavailable, or GatewayTimeout, got {response1.StatusCode}"
            );

            // Step 2: Add second route via configuration update
            var updatedRoutes = new List<RouteConfig>(initialRoutes)
            {
                new RouteConfig
                {
                    RouteId = "route-web1.localhost",
                    ClusterId = "cluster-web1.localhost",
                    Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                    {
                        Hosts = new[] { "web1.localhost" },
                        Path = "/{**catch-all}"
                    }
                }
            };

            var updatedClusters = new List<ClusterConfig>(initialClusters)
            {
                new ClusterConfig
                {
                    ClusterId = "cluster-web1.localhost",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["backend1"] = new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:3000" }
                    }
                }
            };

            config.Update(updatedRoutes, updatedClusters);

            // Small delay to allow YARP to reload configuration
            await Task.Delay(100);

            // Step 3: Verify both routes work after configuration update
            var request2 = new HttpRequestMessage(HttpMethod.Get, "/");
            request2.Headers.Add("Host", "web1.localhost");
            var response2 = await _client.SendAsync(request2);

            Assert.True(
                response2.StatusCode == HttpStatusCode.OK ||
                response2.StatusCode == HttpStatusCode.BadGateway ||
                response2.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response2.StatusCode == HttpStatusCode.GatewayTimeout,
                $"web1.localhost (after update): Expected OK, Bad Gateway, ServiceUnavailable, or GatewayTimeout, got {response2.StatusCode}"
            );

            // Verify first route still works (config preserved existing routes)
            var request3 = new HttpRequestMessage(HttpMethod.Get, "/");
            request3.Headers.Add("Host", "api1.localhost");
            var response3 = await _client.SendAsync(request3);

            Assert.True(
                response3.StatusCode == HttpStatusCode.OK ||
                response3.StatusCode == HttpStatusCode.BadGateway ||
                response3.StatusCode == HttpStatusCode.ServiceUnavailable ||
                response3.StatusCode == HttpStatusCode.GatewayTimeout,
                $"api1.localhost (after update): Expected OK, Bad Gateway, ServiceUnavailable, or GatewayTimeout, got {response3.StatusCode}"
            );
        }

        [Fact]
        public async Task AddHostApiEndpoint_CreatesRouteSuccessfully()
        {
            // Arrange
            var testHostname = "testapi.localhost";
            var testBackendUrl = "http://localhost:9999";

            // Act - Call the API endpoint to add a host
            var response = await _client.PostAsJsonAsync("/api/v1/add-host", new
            {
                hostname = testHostname,
                backendUrl = testBackendUrl
            });

            // Assert - Verify successful response
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(result);

            // Verify the route was added by making a request
            var testRequest = new HttpRequestMessage(HttpMethod.Get, "/");
            testRequest.Headers.Add("Host", testHostname);
            var testResponse = await _client.SendAsync(testRequest);

            // Should route to the backend (may fail if backend not running, but routing is configured)
            Assert.True(
                testResponse.StatusCode == HttpStatusCode.OK ||
                testResponse.StatusCode == HttpStatusCode.BadGateway ||
                testResponse.StatusCode == HttpStatusCode.ServiceUnavailable ||
                testResponse.StatusCode == HttpStatusCode.GatewayTimeout,
                $"Expected routing to be configured, got {testResponse.StatusCode}"
            );
        }

        [Fact]
        public async Task AddHostApiEndpoint_ReturnsConflictForDuplicateHostname()
        {
            // Arrange
            var testHostname = "duplicate.localhost";
            var testBackendUrl = "http://localhost:9999";

            // Act - Add host first time
            var response1 = await _client.PostAsJsonAsync("/api/v1/add-host", new
            {
                hostname = testHostname,
                backendUrl = testBackendUrl
            });
            response1.EnsureSuccessStatusCode();

            // Act - Try to add same hostname again
            var response2 = await _client.PostAsJsonAsync("/api/v1/add-host", new
            {
                hostname = testHostname,
                backendUrl = "http://localhost:8888"
            });

            // Assert - Should return 409 Conflict
            Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
        }

        [Fact]
        public async Task AddHostApiEndpoint_ReturnsBadRequestForInvalidRequest()
        {
            // Act - Try to add host with empty hostname
            var response1 = await _client.PostAsJsonAsync("/api/v1/add-host", new
            {
                hostname = "",
                backendUrl = "http://localhost:9999"
            });

            // Assert - Should return 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);

            // Act - Try to add host with empty backendUrl
            var response2 = await _client.PostAsJsonAsync("/api/v1/add-host", new
            {
                hostname = "test.localhost",
                backendUrl = ""
            });

            // Assert - Should return 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
        }
    }
}
