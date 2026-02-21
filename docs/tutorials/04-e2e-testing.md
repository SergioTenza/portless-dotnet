# Tutorial 4: E2E Testing with Stable URLs

This tutorial demonstrates how to use Portless.NET for end-to-end (E2E) testing with stable .localhost URLs, eliminating flaky tests caused by port conflicts and dynamic URL changes.

## Prerequisites

- .NET 10 SDK installed
- Portless.NET installed: `dotnet tool install --global Portless.NET.Tool`
- Familiarity with E2E testing frameworks (Playwright, Selenium, etc.)

## Overview

E2E testing with Portless provides:
- **Stable URLs** - Tests always use consistent `.localhost` URLs
- **No port conflicts** - Each test run gets unique ports automatically
- **Parallel execution** - Multiple test suites run simultaneously
- **CI/CD friendly** - Deterministic URLs across all environments

## Why Stable URLs Matter for E2E Tests

### The Problem with Dynamic Ports

Traditional E2E testing with random ports:

```javascript
// ❌ Flaky test - port might change
const baseUrl = `http://localhost:${process.env.TEST_PORT || 5000}`;

test('loads homepage', async ({ page }) => {
  await page.goto(baseUrl);
  // Fails if port changes or is already in use
});
```

**Issues:**
- Port conflicts between test suites
- URL changes between runs
- Tests fail unpredictably
- Hard to run tests in parallel

### The Portless Solution

With Portless:

```javascript
// ✅ Stable URL
const baseUrl = 'http://myapp.localhost';

test('loads homepage', async ({ page }) => {
  await page.goto(baseUrl);
  // Always works, consistent across runs
});
```

**Benefits:**
- Same URL every time
- No port conflicts
- Easy parallel execution
- Reproducible test failures

## Step 1: Configure Test Application

For your test application, ensure it reads the PORT environment variable:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Portless integration
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// ... rest of configuration
```

## Step 2: Set Up Portless for Tests

### Option A: CLI-Based (Simple)

Create a test setup script:

```bash
#!/bin/bash
# tests/setup.sh

# Start Portless proxy
portless proxy start

# Start the test application
portless testapp dotnet run --project src/MyApp/MyApp.csproj &
TESTAPP_PID=$!

# Wait for app to be ready
echo "Waiting for testapp to start..."
sleep 5

# Run tests
dotnet test

# Cleanup
kill $TESTAPP_PID
portless proxy stop
```

### Option B: Programmatic (Recommended)

Create a test fixture that manages Portless lifecycle:

```csharp
// Tests/PortlessFixture.cs
public class PortlessFixture : IAsyncLifetime
{
    private Process _proxyProcess = null!;
    private Process _appProcess = null!;
    public string BaseUrl => "http://testapp.localhost";

    public async Task InitializeAsync()
    {
        // Start Portless proxy
        _proxyProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "portless",
            Arguments = "proxy start",
            RedirectStandardOutput = true,
            UseShellExecute = false
        })!;

        await Task.Delay(2000); // Wait for proxy to start

        // Start test application
        _appProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "portless",
            Arguments = "testapp dotnet run --project src/MyApp/MyApp.csproj",
            RedirectStandardOutput = true,
            UseShellExecute = false
        })!;

        await Task.Delay(5000); // Wait for app to be ready
    }

    public async Task DisposeAsync()
    {
        _appProcess?.Kill();
        _appProcess?.WaitForExit();
        _proxyProcess?.Kill();
        _proxyProcess?.WaitForExit();
    }
}
```

## Step 3: Playwright E2E Tests

Install Playwright:

```bash
dotnet add package Microsoft.Playwright
playwright install
```

Create E2E tests:

```csharp
// Tests/E2E/HomepageTests.cs
using Microsoft.Playwright;
using Xunit;

public class HomepageTests : IClassFixture<PortlessFixture>, IAsyncLifetime
{
    private readonly PortlessFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public HomepageTests(PortlessFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _page.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task LoadHomepage_ShouldDisplayTitle()
    {
        // Arrange
        await _page.GotoAsync(_fixture.BaseUrl);

        // Act
        var title = await _page.TitleAsync();

        // Assert
        Assert.Equal("My App", title);
    }

    [Fact]
    public async Task NavigateToProducts_ShouldDisplayProducts()
    {
        // Arrange
        await _page.GotoAsync(_fixture.BaseUrl);

        // Act
        await _page.ClickAsync("text=Products");
        var productsVisible = await _page.Locator(".product-card").CountAsync();

        // Assert
        Assert.True(productsVisible > 0);
    }

    [Fact]
    public async Task SubmitForm_ShouldCreateOrder()
    {
        // Arrange
        await _page.GotoAsync($"{_fixture.BaseUrl}/orders");

        // Act
        await _page.FillAsync("[name=productId]", "1");
        await _page.FillAsync("[name=quantity]", "2");
        await _page.ClickAsync("button[type=submit]");

        // Assert
        await Expect(_page.Locator(".success-message"))
            .ToBeVisibleAsync();
    }
}
```

## Step 4: Selenium E2E Tests

Install Selenium:

```bash
dotnet add package Selenium.WebDriver
dotnet add package Selenium.Chrome.WebDriver
```

Create Selenium tests:

```csharp
// Tests/E2E/SeleniumTests.cs
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

public class SeleniumTests : IClassFixture<PortlessFixture>, IDisposable
{
    private readonly PortlessFixture _fixture;
    private readonly IWebDriver _driver;

    public SeleniumTests(PortlessFixture fixture)
    {
        _fixture = fixture;
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        _driver = new ChromeDriver(options);
    }

    [Fact]
    public void LoadHomepage_ShouldDisplayTitle()
    {
        // Act
        _driver.Navigate().GoToUrl(_fixture.BaseUrl);
        var title = _driver.Title;

        // Assert
        Assert.Equal("My App", title);
    }

    [Fact]
    public void SearchProducts_ShouldReturnResults()
    {
        // Arrange
        _driver.Navigate().GoToUrl($"{_fixture.BaseUrl}/products");

        // Act
        var searchBox = _driver.FindElement(By.Name("search"));
        searchBox.SendKeys("laptop");
        searchBox.Submit();

        // Assert
        var results = _driver.FindElements(By.ClassName("product-item"));
        Assert.NotEmpty(results);
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}
```

## Step 5: RestAssured.NET API Tests

Install RestAssured.Net:

```bash
dotnet add package RestAssured.Net
```

Create API tests:

```csharp
// Tests/E2E/ApiTests.cs
using RestAssured.Net;
using Xunit;

public class ApiTests : IClassFixture<PortlessFixture>
{
    private readonly PortlessFixture _fixture;

    public ApiTests(PortlessFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void GetProducts_ShouldReturnOk()
    {
        Given()
            .Uri($"{_fixture.BaseUrl}/api/products")
        .When()
            .Get()
        .Then()
            .StatusCode(200)
            .Body("$.length", GreateThan(0));
    }

    [Fact]
    public void CreateOrder_ShouldReturnCreated()
    {
        var order = new { productId = 1, quantity = 2 };

        Given()
            .Uri($"{_fixture.BaseUrl}/api/orders")
            .Body(order)
        .When()
            .Post()
        .Then()
            .StatusCode(201)
            .Body("$.id", NotNull());
    }
}
```

## CI/CD Integration

### GitHub Actions

```yaml
# .github/workflows/e2e-tests.yml
name: E2E Tests

on: [push, pull_request]

jobs:
  e2e:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Install Portless
        run: dotnet tool install --global Portless.NET.Tool

      - name: Start Portless proxy
        run: portless proxy start

      - name: Start test application
        run: |
          portless testapp dotnet run --project src/MyApp/MyApp.csproj &
          sleep 10

      - name: Run E2E tests
        run: dotnet test Tests/E2E

      - name: Cleanup
        if: always()
        run: portless proxy stop
```

### Azure Pipelines

```yaml
# azure-pipelines.yml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  displayName: 'Install .NET 10'
  inputs:
    packageType: 'sdk'
    version: '10.0.x'

- script: |
    dotnet tool install --global Portless.NET.Tool
  displayName: 'Install Portless'

- script: portless proxy start
  displayName: 'Start Portless proxy'

- script: |
    portless testapp dotnet run --project src/MyApp/MyApp.csproj &
    sleep 10
  displayName: 'Start test application'

- script: dotnet test Tests/E2E
  displayName: 'Run E2E tests'

- script: portless proxy stop
  displayName: 'Stop Portless'
  condition: always()
```

## Parallel Test Execution

Run multiple test suites simultaneously:

```bash
# Terminal 1
portless testapp-1 dotnet run &
dotnet test --filter "FullyQualifiedName~HomePageTests"

# Terminal 2
portless testapp-2 dotnet run &
dotnet test --filter "FullyQualifiedName~ProductTests"

# Terminal 3
portless testapp-3 dotnet run &
dotnet test --filter "FullyQualifiedName~OrderTests"
```

Each test suite uses a different URL:
- `http://testapp-1.localhost`
- `http://testapp-2.localhost`
- `http://testapp-3.localhost`

## Test Data Management

### Reset Database Between Tests

```csharp
// Tests/E2E/DatabaseFixture.cs
public class DatabaseFixture
{
    public void ResetDatabase()
    {
        // Reset database to known state
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }
}
```

Use in tests:

```csharp
public class OrderTests : IClassFixture<PortlessFixture>, IClassFixture<DatabaseFixture>
{
    private readonly PortlessFixture _portless;
    private readonly DatabaseFixture _db;

    public OrderTests(PortlessFixture portless, DatabaseFixture db)
    {
        _portless = portless;
        _db = db;
    }

    [Fact]
    public async Task CreateOrder_ShouldSucceed()
    {
        _db.ResetDatabase(); // Fresh state

        // Test code...
    }
}
```

## Best Practices

1. **Always use stable URLs** - Never hardcode ports in tests
2. **Clean up resources** - Use `IAsyncLifetime` for proper disposal
3. **Wait for readiness** - Add delays or health checks before running tests
4. **Isolate test data** - Reset database between tests
5. **Run tests in parallel** - Use unique hostnames for parallel execution
6. **Mock external services** - Don't call real APIs in E2E tests
7. **Use fixtures** - Share Portless setup across tests

## Troubleshooting

### Tests fail with connection refused

**Solution:** Ensure application is fully started before tests run:
```csharp
await Task.Delay(5000); // Simple delay
// Or use health check:
var response = await httpClient.GetAsync("http://testapp.localhost/health");
while (!response.IsSuccessStatusCode)
{
    await Task.Delay(1000);
    response = await httpClient.GetAsync("http://testapp.localhost/health");
}
```

### Tests work locally but fail in CI

**Solution:** Ensure Portless is installed in CI:
```yaml
- name: Install Portless
  run: dotnet tool install --global Portless.NET.Tool
```

### Flaky tests due to timing

**Solution:** Use explicit waits:
```javascript
// Playwright
await page.waitForSelector('.loaded');

// Selenium
new WebDriverWait(driver, TimeSpan.FromSeconds(10))
    .Until(d => d.FindElement(By.ClassName("loaded")));
```

## Next Steps

- [Tutorial 1: Migration Guide](01-migration.md) - Set up your application
- [Tutorial 3: Microservices](03-microservices.md) - Test service communication
- [Integration Guides](../integration/) - Advanced configuration

## Example Project

A complete E2E testing example is available in the `Examples/E2ETests` directory of the Portless.NET repository.
