using System.Collections.Concurrent;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Thread-safe ring-buffer implementation of <see cref="IRequestInspector"/>.
/// Oldest items are automatically evicted when capacity is reached.
/// </summary>
public sealed class RequestInspectorService : IRequestInspector
{
    private readonly int _capacity;
    private readonly List<CapturedRequest> _buffer;
    private readonly ConcurrentDictionary<Guid, CapturedRequest> _index;
    private readonly object _lock = new();
    private int _head; // next write position when buffer is full

    public RequestInspectorService(int capacity = 1000)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        _capacity = capacity;
        _buffer = new List<CapturedRequest>(capacity);
        _index = new ConcurrentDictionary<Guid, CapturedRequest>();
    }

    public void Capture(CapturedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_lock)
        {
            if (_buffer.Count < _capacity)
            {
                _buffer.Add(request);
            }
            else
            {
                // Evict oldest item at the _head position
                var old = _buffer[_head];
                _index.TryRemove(old.Id, out _);
                _buffer[_head] = request;
                _head = (_head + 1) % _capacity;
            }

            _index[request.Id] = request;
        }
    }

    public IReadOnlyList<CapturedRequest> GetRecent(int count = 100)
    {
        if (count <= 0)
            return [];

        lock (_lock)
        {
            return _buffer
                .OrderByDescending(r => r.Timestamp)
                .Take(count)
                .ToList()
                .AsReadOnly();
        }
    }

    public CapturedRequest? GetById(Guid id)
        => _index.TryGetValue(id, out var request) ? request : null;

    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
            _index.Clear();
            _head = 0;
        }
    }

    public int Count
    {
        get { lock (_lock) { return _buffer.Count; } }
    }
}
