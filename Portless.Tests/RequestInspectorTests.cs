using Portless.Core.Models;
using Portless.Core.Services;

namespace Portless.Tests;

public sealed class RequestInspectorTests
{
    private static CapturedRequest CreateRequest(
        string method = "GET",
        string host = "test.localhost",
        string path = "/",
        int status = 200,
        long duration = 50) => new()
    {
        Method = method,
        Hostname = host,
        Path = path,
        StatusCode = status,
        DurationMs = duration,
        Timestamp = DateTime.UtcNow
    };

    // 1. Capture one request → Count==1, GetRecent returns it
    [Fact]
    public void Capture_SingleRequest_StoredInBuffer()
    {
        var sut = new RequestInspectorService(capacity: 10);
        var req = CreateRequest();

        sut.Capture(req);

        Assert.Equal(1, sut.Count);
        var recent = sut.GetRecent();
        Assert.Single(recent);
        Assert.Equal(req.Id, recent[0].Id);
    }

    // 2. Capture 5 requests → Count==5, GetRecent returns all
    [Fact]
    public void Capture_MultipleRequests_AllStored()
    {
        var sut = new RequestInspectorService(capacity: 10);
        var requests = Enumerable.Range(0, 5).Select(_ => CreateRequest()).ToList();

        foreach (var r in requests)
            sut.Capture(r);

        Assert.Equal(5, sut.Count);
        var recent = sut.GetRecent();
        Assert.Equal(5, recent.Count);
    }

    // 3. Capacity=3, capture 5 → Count==3, first two evicted
    [Fact]
    public void Capture_ExceedsCapacity_OldestEvicted()
    {
        var sut = new RequestInspectorService(capacity: 3);
        var requests = Enumerable.Range(0, 5).Select(i => CreateRequest(path: $"/r{i}")).ToList();

        foreach (var r in requests)
            sut.Capture(r);

        Assert.Equal(3, sut.Count);

        // The last 3 requests should remain in the buffer
        var remaining = sut.GetRecent();
        var remainingPaths = remaining.Select(r => r.Path).ToHashSet();
        Assert.Contains("/r2", remainingPaths);
        Assert.Contains("/r3", remainingPaths);
        Assert.Contains("/r4", remainingPaths);

        // The first two should have been evicted from the index
        Assert.Null(sut.GetById(requests[0].Id));
        Assert.Null(sut.GetById(requests[1].Id));

        // The remaining three should still be accessible by id
        Assert.NotNull(sut.GetById(requests[2].Id));
        Assert.NotNull(sut.GetById(requests[3].Id));
        Assert.NotNull(sut.GetById(requests[4].Id));
    }

    // 4. Capture one, GetById returns it
    [Fact]
    public void GetById_ExistingRequest_ReturnsRequest()
    {
        var sut = new RequestInspectorService(capacity: 10);
        var req = CreateRequest(method: "POST", path: "/api/data");
        sut.Capture(req);

        var result = sut.GetById(req.Id);

        Assert.NotNull(result);
        Assert.Equal(req.Id, result!.Id);
        Assert.Equal("POST", result.Method);
        Assert.Equal("/api/data", result.Path);
    }

    // 5. GetById with random Guid returns null
    [Fact]
    public void GetById_Nonexistent_ReturnsNull()
    {
        var sut = new RequestInspectorService(capacity: 10);

        var result = sut.GetById(Guid.NewGuid());

        Assert.Null(result);
    }

    // 6. Capture 5, Clear, Count==0
    [Fact]
    public void Clear_RemovesAllRequests()
    {
        var sut = new RequestInspectorService(capacity: 10);
        foreach (var _ in Enumerable.Range(0, 5))
            sut.Capture(CreateRequest());

        Assert.Equal(5, sut.Count);

        sut.Clear();

        Assert.Equal(0, sut.Count);
        Assert.Empty(sut.GetRecent());
    }

    // 7. Capture 3 with staggered timestamps → GetRecent returns newest first
    [Fact]
    public void GetRecent_ReturnsNewestFirst()
    {
        var sut = new RequestInspectorService(capacity: 10);
        var baseTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var req1 = CreateTimestampedRequest(path: "/oldest", baseTime);
        var req2 = CreateTimestampedRequest(path: "/middle", baseTime.AddSeconds(1));
        var req3 = CreateTimestampedRequest(path: "/newest", baseTime.AddSeconds(2));

        sut.Capture(req1);
        sut.Capture(req2);
        sut.Capture(req3);

        var recent = sut.GetRecent();

        Assert.Equal(3, recent.Count);
        Assert.Equal("/newest", recent[0].Path);
        Assert.Equal("/middle", recent[1].Path);
        Assert.Equal("/oldest", recent[2].Path);
    }

    private static CapturedRequest CreateTimestampedRequest(string path, DateTime timestamp) => new()
    {
        Method = "GET",
        Hostname = "test.localhost",
        Path = path,
        StatusCode = 200,
        DurationMs = 50,
        Timestamp = timestamp
    };

    // 8. Capture 10, GetRecent(3) returns exactly 3
    [Fact]
    public void GetRecent_RespectsCount()
    {
        var sut = new RequestInspectorService(capacity: 20);
        foreach (var i in Enumerable.Range(0, 10))
            sut.Capture(CreateRequest(path: $"/r{i}"));

        var recent = sut.GetRecent(3);

        Assert.Equal(3, recent.Count);
    }

    // 9. Count reflects buffer size at each stage
    [Fact]
    public void Count_ReflectsBufferSize()
    {
        var sut = new RequestInspectorService(capacity: 10);

        Assert.Equal(0, sut.Count);

        for (int i = 0; i < 5; i++)
            sut.Capture(CreateRequest());

        Assert.Equal(5, sut.Count);
    }

    // 10. Concurrent captures from multiple threads → no corruption, all items accounted for
    [Fact]
    public void Capture_ThreadIdSafe()
    {
        const int capacity = 500;
        const int totalWrites = 1000;
        var sut = new RequestInspectorService(capacity: capacity);

        Parallel.For(0, totalWrites, _ =>
        {
            sut.Capture(CreateRequest());
        });

        // Count must never exceed capacity
        Assert.True(sut.Count <= capacity);
        Assert.Equal(capacity, sut.Count);

        // GetRecent with explicit count must return all items in buffer
        var recent = sut.GetRecent(capacity);
        Assert.Equal(capacity, recent.Count);

        // No duplicates in the index
        var distinctIds = recent.Select(r => r.Id).Distinct().Count();
        Assert.Equal(capacity, distinctIds);
    }
}
