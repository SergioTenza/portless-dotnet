# Tutorial 3: Microservices Scenario with Portless

This tutorial demonstrates how to use Portless.NET to run multiple microservices locally, each with a stable .localhost URL, enabling realistic service-to-service communication during development.

## Prerequisites

- .NET 10 SDK installed
- Portless.NET installed: `dotnet tool install --global Portless.NET.Tool`
- Basic understanding of microservices architecture

## Overview

In a microservices architecture, you typically have multiple services that need to communicate with each other. Portless makes this easy by providing:
- Stable URLs for each service (e.g., `http://api.localhost`, `http://frontend.localhost`)
- Automatic port assignment (no port conflicts)
- Service discovery via predictable hostnames

## Architecture Example

We'll build a simple e-commerce microservices architecture:

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Frontend      │────▶│   API Gateway   │────▶│   Products      │
│   (Blazor)      │     │   (Web API)     │     │   (Web API)     │
│ frontend.       │     │ api.localhost   │     │ products.       │
│ localhost       │     │                 │     │ localhost       │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                                │
                                ▼
                         ┌─────────────────┐
                         │   Orders        │
                         │   (Web API)     │
                         │   orders.       │
                         │   localhost     │
                         └─────────────────┘
```

## Step 1: Create the Services

Create a directory structure for your microservices:

```bash
# Create root directory
mkdir MicroservicesDemo
cd MicroservicesDemo

# Create service projects
dotnet new webapi -n ProductsService
dotnet new webapi -n OrdersService
dotnet new webapi -n ApiGateway

# Create a Blazor web app for frontend
dotnet new blazor -n Frontend
```

## Step 2: Configure Each Service for Portless

For each service (`ProductsService`, `OrdersService`, `ApiGateway`), update `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Portless integration
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for service-to-service communication
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Step 3: Create Service Endpoints

### Products Service

Create `Controllers/ProductsController.cs`:

```csharp
[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> Products = new()
    {
        new Product(1, "Laptop", 999.99m),
        new Product(2, "Mouse", 29.99m),
        new Product(3, "Keyboard", 79.99m)
    };

    [HttpGet]
    public IActionResult GetAll() => Ok(Products);

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        return product != null ? Ok(product) : NotFound();
    }
}

public record Product(int Id, string Name, decimal Price);
```

### Orders Service

Create `Controllers/OrdersController.cs`:

```csharp
[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OrdersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // Validate product exists by calling Products Service
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(
            $"http://products.localhost/Products/{request.ProductId}");

        if (!response.IsSuccessStatusCode)
        {
            return BadRequest("Product not found");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            CreatedAt = DateTime.UtcNow
        };

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpGet("{id}")]
    public IActionResult GetOrder(Guid id)
    {
        // Return mock order
        var order = new Order
        {
            Id = id,
            ProductId = 1,
            Quantity = 1,
            CreatedAt = DateTime.UtcNow
        };
        return Ok(order);
    }
}

public class CreateOrderRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### API Gateway

Create `Controllers/ProxyController.cs`:

```csharp
[ApiController]
[Route("[controller]")]
public class ProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProxyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync("http://products.localhost/Products");
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync($"http://products.localhost/Products/{id}");
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await httpClient.PostAsync(
            "http://orders.localhost/Orders",
            content);

        var responseContent = await response.Content.ReadAsStringAsync();
        return Content(responseContent, "application/json");
    }
}

public class CreateOrderRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
```

## Step 4: Configure Frontend for Service Communication

In the Frontend Blazor app, register HttpClient for each service:

```csharp
// Program.cs in Frontend project
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("http://api.localhost/") });
```

Now you can call services from Blazor components:

```razor
@inject HttpClient Http

<button @onclick="LoadProducts">Load Products</button>

@code {
    private List<Product> _products = new();

    private async Task LoadProducts()
    {
        _products = await Http.GetFromJsonAsync<List<Product>>("Proxy/products");
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
```

## Step 5: Run All Services with Portless

First, start the Portless proxy:

```bash
portless proxy start
```

Then, start each service in a separate terminal:

```bash
# Terminal 1: Products Service
cd MicroservicesDemo/ProductsService
portless products dotnet run

# Terminal 2: Orders Service
cd MicroservicesDemo/OrdersService
portless orders dotnet run

# Terminal 3: API Gateway
cd MicroservicesDemo/ApiGateway
portless api dotnet run

# Terminal 4: Frontend
cd MicroservicesDemo/Frontend
portless frontend dotnet run
```

## Step 6: Verify the Architecture

Check all services are registered:

```bash
portless list
```

Expected output:
```
┌───────────┬───────┬─────────┬───────┐
│ Hostname  │ Port  │ Process │ PID   │
├───────────┼───────┼─────────┼───────┤
│ products  │ 4001  │ dotnet  │ 12345 │
│ orders    │ 4002  │ dotnet  │ 12346 │
│ api       │ 4003  │ dotnet  │ 12347 │
│ frontend  │ 4004  │ dotnet  │ 12348 │
└───────────┴───────┴─────────┴───────┘
```

Test service-to-service communication:

```bash
# Get all products through the API Gateway
curl http://api.localhost/Proxy/products

# Create an order through the API Gateway
curl -X POST http://api.localhost/Proxy/orders \
  -H "Content-Type: application/json" \
  -d '{"productId": 1, "quantity": 2}'
```

Access the frontend at: `http://frontend.localhost`

## Managing Multiple Services

### Service Discovery

Portless acts as a service registry:
- Each service gets a stable hostname
- Services communicate via `.localhost` URLs
- No need for hardcoded ports

### Service Lifecycle

When a service crashes or is stopped:
```bash
# Stop a service
# Press Ctrl+C in the service terminal

# Portless automatically cleans up the route
portless list  # Service no longer appears

# Restart with the same hostname
portless products dotnet run
# Gets a new port, same URL (http://products.localhost)
```

### Scaling Services

To run multiple instances of a service:
```bash
# Terminal 1
portless products-1 dotnet run

# Terminal 2
portless products-2 dotnet run

# Both instances accessible via different URLs
# http://products-1.localhost
# http://products-2.localhost
```

## Advanced Configuration

### Service Configuration via appsettings.json

For configuration-based PORT integration, see [appsettings.json Integration](../integration/appsettings.md).

### Health Checks

Add health checks to each service:

```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

Test health via Portless:
```bash
curl http://products.localhost/health
```

### Service Versioning

Run multiple versions side-by-side:
```bash
portless api-v1 dotnet run
portless api-v2 dotnet run

# Access different versions
curl http://api-v1.localhost/Proxy/products
curl http://api-v2.localhost/Proxy/products
```

## Troubleshooting

### Service can't reach another service

**Symptom:** HTTP 502 or connection refused

**Solution:**
1. Verify target service is running: `portless list`
2. Check hostname is correct (e.g., `products.localhost`, not `localhost`)
3. Ensure proxy is running: `portless proxy status`

### Port conflicts

**Symptom:** "Port already in use" error

**Solution:** Portless automatically assigns unique ports. If you see conflicts, ensure you're using unique hostnames:
```bash
# Correct: different hostnames
portless api-v1 dotnet run
portless api-v2 dotnet run

# Incorrect: same hostname will cause conflicts
```

### Too many terminals

**Solution:** Use a terminal multiplexer like tmux or Windows Terminal, or create a start script:

```bash
#!/bin/bash
# start-all.sh
portless proxy start &
sleep 2

(cd ProductsService && portless products dotnet run &) &
(cd OrdersService && portless orders dotnet run &) &
(cd ApiGateway && portless api dotnet run &) &
(cd Frontend && portless frontend dotnet run &) &

wait
```

## Best Practices

1. **Use descriptive hostnames** - Reflect service purpose (e.g., `products`, `orders`, `api`)
2. **Document service dependencies** - Maintain a service catalog
3. **Implement health checks** - Easy service monitoring
4. **Use API Gateway pattern** - Single entry point for frontend
5. **Isolate service state** - Each service owns its data
6. **Implement circuit breakers** - Handle service failures gracefully

## Next Steps

- [Tutorial 4: E2E Testing](04-e2e-testing.md) - Test microservices with stable URLs
- [Integration Guides](../integration/) - Advanced configuration
- [Example Projects](../../Examples/) - Complete working examples

## Example Project

A complete microservices demo is available in the `Examples/Microservices` directory of the Portless.NET repository.
