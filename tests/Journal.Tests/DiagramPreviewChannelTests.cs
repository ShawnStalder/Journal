using Journal.Core.Rendering;

namespace Journal.Tests;

public sealed class DiagramPreviewChannelTests
{
    [Fact]
    public void Publish_StoresLatestTextAndRaisesEvent()
    {
        var channel = new DiagramPreviewChannel();
        var sessionId = Guid.NewGuid();
        var received = new List<(Guid SessionId, string Text)>();
        channel.TextPublished += (id, text) => received.Add((id, text));

        channel.Publish(sessionId, "first");
        channel.Publish(sessionId, "second");

        Assert.Equal("second", channel.GetLatestText(sessionId));
        Assert.Equal(2, received.Count);
        Assert.Equal((sessionId, "second"), received[1]);
    }

    [Fact]
    public void GetLatestText_ForUnknownSession_IsEmpty()
    {
        var channel = new DiagramPreviewChannel();

        Assert.Equal(string.Empty, channel.GetLatestText(Guid.NewGuid()));
    }

    [Fact]
    public void Sessions_AreIndependent()
    {
        var channel = new DiagramPreviewChannel();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        channel.Publish(first, "a");
        channel.Publish(second, "b");

        Assert.Equal("a", channel.GetLatestText(first));
        Assert.Equal("b", channel.GetLatestText(second));
    }

    [Fact]
    public void SetDetached_RaisesEventOnlyWhenStateChanges()
    {
        var channel = new DiagramPreviewChannel();
        var sessionId = Guid.NewGuid();
        var changes = new List<bool>();
        channel.DetachedChanged += (_, isDetached) => changes.Add(isDetached);

        channel.SetDetached(sessionId, true);
        channel.SetDetached(sessionId, true);
        channel.SetDetached(sessionId, false);
        channel.SetDetached(sessionId, false);

        Assert.Equal([true, false], changes);
        Assert.False(channel.IsDetached(sessionId));
    }

    [Fact]
    public void Remove_ForgetsTextAndDetachedState()
    {
        var channel = new DiagramPreviewChannel();
        var sessionId = Guid.NewGuid();
        channel.Publish(sessionId, "text");
        channel.SetDetached(sessionId, true);

        channel.Remove(sessionId);

        Assert.Equal(string.Empty, channel.GetLatestText(sessionId));
        Assert.False(channel.IsDetached(sessionId));
    }
}
