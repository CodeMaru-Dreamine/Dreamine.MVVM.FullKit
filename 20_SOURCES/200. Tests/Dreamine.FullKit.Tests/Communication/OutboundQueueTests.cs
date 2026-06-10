using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Abstractions.Options;
using Dreamine.Communication.Core.Queues;

namespace Dreamine.FullKit.Tests.Communication;

public sealed class OutboundQueueTests
{
    [Fact]
    public async Task Queue_DropsOldestWhenFullAddingToBack()
    {
        var queue = new InMemoryOutboundMessageQueue(new OutboundQueueOptions
        {
            MaxQueueSize = 2,
            DropOldestWhenFull = true
        });

        await queue.EnqueueAsync(Create("one"));
        await queue.EnqueueAsync(Create("two"));
        await queue.EnqueueAsync(Create("three"));

        Assert.Equal("two", (await queue.TryDequeueAsync())!.Message.Name);
        Assert.Equal("three", (await queue.TryDequeueAsync())!.Message.Name);
        Assert.Null(await queue.TryDequeueAsync());
    }

    [Fact]
    public async Task Queue_DropsNewestWhenFullAddingToFront()
    {
        var queue = new InMemoryOutboundMessageQueue(new OutboundQueueOptions
        {
            MaxQueueSize = 2,
            DropOldestWhenFull = true
        });

        await queue.EnqueueAsync(Create("one"));
        await queue.EnqueueAsync(Create("two"));
        await queue.EnqueueFrontAsync(Create("zero"));

        Assert.Equal("zero", (await queue.TryDequeueAsync())!.Message.Name);
        Assert.Equal("one", (await queue.TryDequeueAsync())!.Message.Name);
    }

    [Fact]
    public async Task Queue_ThrowsWhenFullAndDroppingDisabled()
    {
        var queue = new InMemoryOutboundMessageQueue(new OutboundQueueOptions
        {
            MaxQueueSize = 1,
            DropOldestWhenFull = false
        });

        await queue.EnqueueAsync(Create("one"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await queue.EnqueueAsync(Create("two")));
    }

    [Fact]
    public void QueuedOutboundMessage_TracksExpirationAndFailure()
    {
        var queued = new QueuedOutboundMessage(
            new MessageEnvelope { Name = "Job" },
            new DateTimeOffset(2026, 6, 7, 0, 0, 0, TimeSpan.Zero),
            attemptCount: 2,
            lastError: null);

        var failed = queued.WithFailure(new InvalidOperationException("bad"));

        Assert.False(queued.IsExpired(null, queued.EnqueuedAt.AddDays(1)));
        Assert.True(queued.IsExpired(TimeSpan.FromSeconds(1), queued.EnqueuedAt.AddSeconds(2)));
        Assert.Equal(3, failed.AttemptCount);
        Assert.Equal("bad", failed.LastError);
        Assert.Same(queued.Message, failed.Message);
    }

    private static QueuedOutboundMessage Create(string name)
    {
        return new QueuedOutboundMessage(new MessageEnvelope { Name = name });
    }
}
