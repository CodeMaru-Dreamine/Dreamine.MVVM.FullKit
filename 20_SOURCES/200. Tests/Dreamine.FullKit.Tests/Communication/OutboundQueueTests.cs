using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Abstractions.Options;
using Dreamine.Communication.Core.Queues;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// \if KO
/// <para>Outbound Queue Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates outbound queue tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class OutboundQueueTests
{
    /// <summary>
    /// \if KO
    /// <para>Queue Drops Oldest When Full Adding To Back 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the queue drops oldest when full adding to back operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Queue Drops Oldest When Full Adding To Back 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the queue drops oldest when full adding to back operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Queue Drops Newest When Full Adding To Front 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the queue drops newest when full adding to front operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Queue Drops Newest When Full Adding To Front 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the queue drops newest when full adding to front operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Queue Throws When Full And Dropping Disabled 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the queue throws when full and dropping disabled operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Queue Throws When Full And Dropping Disabled 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the queue throws when full and dropping disabled operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Queued Outbound Message Tracks Expiration And Failure 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the queued outbound message tracks expiration and failure operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the value.</para>
    /// \endif
    /// </summary>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create 작업에서 생성한 <c>QueuedOutboundMessage</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>QueuedOutboundMessage</c> result produced by the create operation.</para>
    /// \endif
    /// </returns>
    private static QueuedOutboundMessage Create(string name)
    {
        return new QueuedOutboundMessage(new MessageEnvelope { Name = name });
    }
}
