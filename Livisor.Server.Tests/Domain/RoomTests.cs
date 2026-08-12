using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;

namespace Livisor.Server.Tests.Domain;

public class RoomTests
{
    private static Timeline BuildTimeline(string time = "10:00:00:00")
        => Timeline.Create([new TimelineItem(PlaybackTime.Parse(time), ActionType.Start, 1)]);

    [Fact]
    public void Create_ReturnsRoomWithNoCurrentTimeline()
    {
        var room = Room.Create(RoomId.Create("room1"));

        Assert.Equal("room1", room.Id.Value);
        Assert.Null(room.Current);
    }

    [Fact]
    public void SetCurrent_SetsCurrentTimeline()
    {
        var room = Room.Create(RoomId.Create("room1"));
        var t1 = BuildTimeline("10:00:00:00");

        var updated = room.SetCurrent(t1);

        Assert.Same(t1, updated.Current);
    }

    [Fact]
    public void SetCurrent_ReplacesPreviousTimeline()
    {
        // BroadcastTimelineAsync の1回は「今の演目を丸ごと差し替える」イベントであり、
        // 過去の演目は残さない。
        var room = Room.Create(RoomId.Create("room1"));
        var t1 = BuildTimeline("10:00:00:00");
        var t2 = BuildTimeline("11:00:00:00");

        var updated = room.SetCurrent(t1).SetCurrent(t2);

        Assert.Same(t2, updated.Current);
    }

    [Fact]
    public void SetCurrent_DoesNotMutateOriginalInstance()
    {
        // Room は不変。SetCurrent は新しいインスタンスを返し、元のインスタンスは変わらない。
        var room = Room.Create(RoomId.Create("room1"));

        var updated = room.SetCurrent(BuildTimeline());

        Assert.Null(room.Current);
        Assert.NotNull(updated.Current);
    }
}
