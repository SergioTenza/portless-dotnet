using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Portless.Core.Services;

public sealed class EventBus : IEventBus, IDisposable
{
    private const int SubscriberChannelCapacity = 1000;

    private readonly List<Channel<EventBusEvent>> _subscribers = [];
    private readonly Lock _lock = new();

    public void Publish(string eventType, object? data = null)
    {
        var evt = new EventBusEvent(eventType, data, DateTime.UtcNow);

        List<Channel<EventBusEvent>> snapshot;
        lock (_lock)
        {
            snapshot = [.. _subscribers];
        }

        foreach (var channel in snapshot)
        {
            channel.Writer.TryWrite(evt);
        }
    }

    public async IAsyncEnumerable<EventBusEvent> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<EventBusEvent>(new BoundedChannelOptions(SubscriberChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        lock (_lock)
        {
            _subscribers.Add(channel);
        }

        var enumerator = new ChannelEnumerator(channel, cancellationToken);
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                yield return enumerator.Current;
            }
        }
        finally
        {
            lock (_lock)
            {
                _subscribers.Remove(channel);
            }

            channel.Writer.TryComplete();
        }
    }

    public void Dispose()
    {
        List<Channel<EventBusEvent>> snapshot;
        lock (_lock)
        {
            snapshot = [.. _subscribers];
            _subscribers.Clear();
        }

        foreach (var channel in snapshot)
        {
            channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Wrapper around ChannelReader that catches OperationCanceledException,
    /// since yield cannot be used inside a try-catch block.
    /// </summary>
    private sealed class ChannelEnumerator : IAsyncEnumerator<EventBusEvent>
    {
        private readonly ChannelReader<EventBusEvent> _reader;
        private readonly CancellationToken _cancellationToken;

        public ChannelEnumerator(Channel<EventBusEvent> channel, CancellationToken cancellationToken)
        {
            _reader = channel.Reader;
            _cancellationToken = cancellationToken;
        }

        public EventBusEvent Current { get; private set; } = default!;

        public async ValueTask<bool> MoveNextAsync()
        {
            try
            {
                var result = await _reader.WaitToReadAsync(_cancellationToken);
                if (result && _reader.TryRead(out var item))
                {
                    Current = item;
                    return true;
                }
                return false;
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
