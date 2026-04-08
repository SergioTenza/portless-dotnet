using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>In-memory request inspector for capturing proxy traffic.</summary>
public interface IRequestInspector
{
    void Capture(CapturedRequest request);
    IReadOnlyList<CapturedRequest> GetRecent(int count = 100);
    CapturedRequest? GetById(Guid id);
    void Clear();
    int Count { get; }
}
