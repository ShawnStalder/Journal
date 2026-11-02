namespace Journal.Core.Rendering;

/// <summary>
/// Carries the latest diagram text from an editor to its pop-out preview window, and records whether
/// that preview is currently popped out. Each editor session is identified by its own id.
/// </summary>
public sealed class DiagramPreviewChannel
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, string> _latestText = [];
    private readonly HashSet<Guid> _detached = [];

    public event Action<Guid, string>? TextPublished;

    public event Action<Guid, bool>? DetachedChanged;

    public void Publish(Guid sessionId, string text)
    {
        lock (_lock)
        {
            _latestText[sessionId] = text;
        }

        TextPublished?.Invoke(sessionId, text);
    }

    public string GetLatestText(Guid sessionId)
    {
        lock (_lock)
        {
            return _latestText.GetValueOrDefault(sessionId, string.Empty);
        }
    }

    public bool IsDetached(Guid sessionId)
    {
        lock (_lock)
        {
            return _detached.Contains(sessionId);
        }
    }

    public void SetDetached(Guid sessionId, bool isDetached)
    {
        bool changed;
        lock (_lock)
        {
            changed = isDetached ? _detached.Add(sessionId) : _detached.Remove(sessionId);
        }

        if (changed)
        {
            DetachedChanged?.Invoke(sessionId, isDetached);
        }
    }

    public void Remove(Guid sessionId)
    {
        lock (_lock)
        {
            _latestText.Remove(sessionId);
            _detached.Remove(sessionId);
        }
    }
}
