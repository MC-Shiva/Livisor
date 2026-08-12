using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;

namespace Livisor.Server.Tests.Domain;

public class TimelineTests
{
    [Fact]
    public void Create_Null_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Timeline.Create(null!));
    }

    [Fact]
    public void Create_EmptyList_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Timeline.Create([]));
    }

    [Fact]
    public void Create_WithItems_ReturnsTimeline()
    {
        var items = new[] { new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.Start, 1) };
        var timeline = Timeline.Create(items);
        Assert.Equal(items, timeline.Items);
    }

    [Fact]
    public void Create_MultipleItems_PreservesAll()
    {
        var items = new[]
        {
            new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.Start, 1),
            new TimelineItem(PlaybackTime.Parse("10:00:03:00"), ActionType.VolumeChange, 10),
            new TimelineItem(PlaybackTime.Parse("10:00:06:00"), ActionType.Stop, 1),
        };
        var timeline = Timeline.Create(items);
        Assert.Equal(3, timeline.Items.Count);
    }

    // --- 時刻順の不変条件 ---

    [Fact]
    public void Create_OrderedItems_ReturnsTimeline()
    {
        var items = new[]
        {
            new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.Start, 1),
            new TimelineItem(PlaybackTime.Parse("10:00:03:00"), ActionType.VolumeChange, 10),
            new TimelineItem(PlaybackTime.Parse("10:00:06:00"), ActionType.Stop, 1),
        };

        var timeline = Timeline.Create(items);

        Assert.Equal(3, timeline.Items.Count);
    }

    [Fact]
    public void Create_SameTimeItems_ReturnsTimeline()
    {
        // 同時刻の複数操作は正当（狭義単調増加ではなく広義単調増加を許可する）。
        var items = new[]
        {
            new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.Start, 1),
            new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.VolumeChange, 10),
        };

        var timeline = Timeline.Create(items);

        Assert.Equal(2, timeline.Items.Count);
    }

    [Fact]
    public void Create_OutOfOrderItems_ThrowsDomainException()
    {
        var items = new[]
        {
            new TimelineItem(PlaybackTime.Parse("10:00:06:00"), ActionType.Stop, 1),
            new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.Start, 1),
        };

        Assert.Throws<DomainException>(() => Timeline.Create(items));
    }
}
