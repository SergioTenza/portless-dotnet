using Portless.Core.Models;
using Portless.Core.Services;

namespace Portless.Tests;

public class RequestInspectorTests
{
    private static CapturedRequest CreateRequest(
        string method = "GET",
        string path = "/",
        string hostname = "localhost",
        int statusCode = 200,
        long durationMs = 50)
    {
        return new CapturedRequest
        {
            Method = method,
            Path = path,
            Hostname = hostname,
            Scheme = "https",
            StatusCode = statusCode,
            DurationMs = durationMs,
            Timestamp = DateTime.UtcNow,
        };
    }

    [Fact]
    public void Capture_SingleRequest_StoredInBuffer()
    {
        // Arrange
        var inspector = new RequestInspectorService(capacity: 10);
        var request = CreateRequest(method: "POST", path: "/api/test");

        // Act
        inspector.Capture(request);

        // Assert
        Assert.Equal(1, inspector.Count);
        var recent = inspector.GetRecent();
        Assert.Single(recent);
        Assert.Equal(request.Id, recent[0].Id);
        Assert.Equal("POST", recent[0].Method);
        Assert.Equal("/api/test", recent[0].Path);
    }

    [Fact]
    public void Capture_ExceedsCapacity_OldestEvicted()
    {
        // Arrange
        const int capacity = 3;
        var inspector = new RequestInspectorService(capacity: capacity);

        var request1 = CreateRequest(path: "/first");
        var request2 = CreateRequest(path: "/second");
        var request3 = CreateRequest(path: "/third");
        var request4 = CreateRequest(path: "/fourth");

        // Act
        inspector.Capture(request1);
        inspector.Capture(request2);
        inspector.Capture(request3);
        // Buffer is full (capacity=3), next capture evicts the oldest
        inspector.Capture(request4);

        // Assert
        Assert.Equal(capacity, inspector.Count);

        // request1 should have been evicted
        Assert.Null(inspector.GetById(request1.Id));

        // request2, request3, request4 should still be present
        Assert.NotNull(inspector.GetById(request2.Id));
        Assert.NotNull(inspector.GetById(request3.Id));
        Assert.NotNull(inspector.GetById(request4.Id));
    }

    [Fact]
    public void GetById_ExistingRequest_ReturnsRequest()
    {
        // Arrange
        var inspector = new RequestInspectorService(capacity: 10);
        var request = CreateRequest(method: "PUT", path: "/api/items/42", statusCode: 204);
        inspector.Capture(request);

        // Act
        var result = inspector.GetById(request.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Id, result!.Id);
        Assert.Equal("PUT", result.Method);
        Assert.Equal("/api/items/42", result.Path);
        Assert.Equal(204, result.StatusCode);
    }

    [Fact]
    public void GetById_Nonexistent_ReturnsNull()
    {
        // Arrange
        var inspector = new RequestInspectorService(capacity: 10);
        var request = CreateRequest();
        inspector.Capture(request);

        // Act
        var result = inspector.GetById(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Clear_RemovesAllRequests()
    {
        // Arrange
        var inspector = new RequestInspectorService(capacity: 10);
        inspector.Capture(CreateRequest());
        inspector.Capture(CreateRequest());
        inspector.Capture(CreateRequest());
        Assert.Equal(3, inspector.Count);

        // Act
        inspector.Clear();

        // Assert
        Assert.Equal(0, inspector.Count);
        Assert.Empty(inspector.GetRecent());
    }

    [Fact]
    public void GetRecent_ReturnsLatestFirst()
    {
        // Arrange
        var inspector = new RequestInspectorService(capacity: 10);

        var request1 = new CapturedRequest
        {
            Path = "/oldest",
            Method = "GET",
            Hostname = "localhost",
            Scheme = "https",
            Timestamp = DateTime.UtcNow.AddSeconds(-3),
        };

        var request2 = new CapturedRequest
        {
            Path = "/middle",
            Method = "GET",
            Hostname = "localhost",
            Scheme = "https",
            Timestamp = DateTime.UtcNow.AddSeconds(-2),
        };

        var request3 = new CapturedRequest
        {
            Path = "/newest",
            Method = "GET",
            Hostname = "localhost",
            Scheme = "https",
            Timestamp = DateTime.UtcNow.AddSeconds(-1),
        };

        inspector.Capture(request1);
        inspector.Capture(request2);
        inspector.Capture(request3);

        // Act
        var recent = inspector.GetRecent(count: 10);

        // Assert
        Assert.Equal(3, recent.Count);
        Assert.Equal("/newest", recent[0].Path);
        Assert.Equal("/middle", recent[1].Path);
        Assert.Equal("/oldest", recent[2].Path);
    }

    [Fact]
    public void Count_ReflectsBufferSize()
    {
        // Arrange
        var inspector = new RequestInspectorService(capacity: 5);

        // Assert initial
        Assert.Equal(0, inspector.Count);

        // Act & Assert - add requests one by one
        for (int i = 0; i < 5; i++)
        {
            inspector.Capture(CreateRequest(path: $"/item/{i}"));
            Assert.Equal(i + 1, inspector.Count);
        }

        // Buffer is full, adding more should not increase count beyond capacity
        inspector.Capture(CreateRequest(path: "/overflow"));
        Assert.Equal(5, inspector.Count);
    }
}
