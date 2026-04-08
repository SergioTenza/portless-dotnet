using Portless.Core.Services;

namespace Portless.Tests.Dashboard;

public sealed class EventBusTests
{
    [Fact]
    public async Task Publish_SingleEvent_SubscriberReceivesIt()
    {
        using var bus = new EventBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Start a subscriber task that collects events
        var subscribeTask = Task.Run(async () =>
        {
            var events = new List<EventBusEvent>();
            await foreach (var evt in bus.SubscribeAsync(cts.Token))
            {
                events.Add(evt);
                if (events.Count >= 1)
                    break;
            }
            return events;
        });

        // Give the subscriber time to start and register its channel
        await Task.Delay(200);

        bus.Publish("TestEvent", new { Message = "hello" });

        var received = await subscribeTask;
        Assert.Single(received);
        Assert.Equal("TestEvent", received[0].Type);
    }

    [Fact]
    public async Task Publish_SingleEvent_AllSubscribersReceiveIt()
    {
        using var bus = new EventBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var subscriberCount = 3;
        var barriers = new TaskCompletionSource[subscriberCount];
        for (int i = 0; i < subscriberCount; i++)
            barriers[i] = new TaskCompletionSource();

        // Start multiple subscriber tasks
        var subscribeTasks = Enumerable.Range(0, subscriberCount).Select(i => Task.Run(async () =>
        {
            var events = new List<EventBusEvent>();
            await foreach (var evt in bus.SubscribeAsync(cts.Token))
            {
                events.Add(evt);
                barriers[i].TrySetResult();
                break;
            }
            return events;
        })).ToArray();

        // Give subscribers time to start
        await Task.Delay(300);

        bus.Publish("MultiTest", new { Value = 42 });

        // Wait for all subscribers with timeout
        var allBarrierTasks = barriers.Select(b => b.Task).ToArray();
        await Task.WhenAll(Task.WhenAll(allBarrierTasks), Task.Delay(3000));

        cts.Cancel();

        var results = await Task.WhenAll(subscribeTasks);

        Assert.All(results, received =>
        {
            Assert.Single(received);
            Assert.Equal("MultiTest", received[0].Type);
        });
    }

    [Fact]
    public async Task SubscribeAsync_CancelledToken_SubscriberStopsGracefully()
    {
        using var bus = new EventBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var subscribeTask = Task.Run(async () =>
        {
            var events = new List<EventBusEvent>();
            await foreach (var evt in bus.SubscribeAsync(cts.Token))
            {
                events.Add(evt);
            }
            return events;
        });

        // Publish one event before cancelling
        await Task.Delay(100);
        bus.Publish("BeforeCancel");

        // Let the cancellation token expire
        await Task.Delay(2500);

        var received = await subscribeTask;
        Assert.Single(received);
        Assert.Equal("BeforeCancel", received[0].Type);
    }

    [Fact]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        using var bus = new EventBus();
        // Should not throw
        bus.Publish("NoSubscribers", new { Test = true });
    }

    [Fact]
    public async Task Dispose_CompletesAllSubscribers()
    {
        var bus = new EventBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var subscribeTask = Task.Run(async () =>
        {
            var events = new List<EventBusEvent>();
            await foreach (var evt in bus.SubscribeAsync(cts.Token))
            {
                events.Add(evt);
            }
            return events;
        });

        await Task.Delay(100);
        bus.Dispose();

        var received = await subscribeTask;
        Assert.Empty(received);
    }
}
