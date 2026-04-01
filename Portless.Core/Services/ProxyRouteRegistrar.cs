using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Portless.Core.Services;

public class ProxyRouteRegistrar : IProxyRouteRegistrar
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyRouteRegistrar> _logger;
    private readonly string _proxyBaseUrl;

    public ProxyRouteRegistrar(
        IHttpClientFactory httpClientFactory,
        ILogger<ProxyRouteRegistrar> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _proxyBaseUrl = "http://localhost:1355";
    }

    public async Task<bool> RegisterRouteAsync(string hostname, string backendUrl)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var payload = new { hostname, backendUrl };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync($"{_proxyBaseUrl}/api/v1/add-host", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to register route {Hostname} with proxy. Status: {Status}, Response: {Response}",
                    hostname, response.StatusCode, errorContent);
                return false;
            }
            _logger.LogInformation("Route {Hostname} registered with proxy", hostname);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with proxy for route registration");
            return false;
        }
    }

    public async Task<bool> RemoveRouteAsync(string hostname)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var payload = new { hostname };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync($"{_proxyBaseUrl}/api/v1/remove-host", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to remove route {Hostname} from proxy. Status: {Status}, Response: {Response}",
                    hostname, response.StatusCode, errorContent);
                return false;
            }
            _logger.LogInformation("Route {Hostname} removed from proxy", hostname);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with proxy for route removal");
            return false;
        }
    }
}
