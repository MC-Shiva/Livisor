using Livisor.Server.Domain;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;

namespace Livisor.Server.Tests.Domain;

public class RoomTests
{
    private static Room NewRoom() => Room.Create(RoomId.Create("room1"));

    private static ScheduledAction BuildAction(string offset = "00:00:05:00")
        => new(PlaybackTime.Parse(offset), ActionType.VolumeChange, 10);

    private static RoomState BuildState(string key, ActionValue value)
        => RoomState.Create([new KeyValuePair<string, ActionValue>(key, value)]);

    [Fact]
    public void Create_ReturnsStoppedRoomWithoutScheduleOrState()
    {
        var room = NewRoom();

        Assert.Equal("room1", room.Id.Value);
        Assert.False(room.Transport.Playing);
        Assert.Empty(room.ScheduledActions);
        Assert.Empty(room.State.Entries);
    }

    [Fact]
    public void Create_NullId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Room.Create(null!));
    }

    [Fact]
    public void Play_StartsTransportAtGivenTime()
    {
        var room = NewRoom().Play(1_000);

        Assert.True(room.Transport.Playing);
        Assert.Equal(1_000, room.Transport.StartedAtUnixMs);
    }

    [Fact]
    public void Play_KeepsScheduleAndState()
    {
        var room = NewRoom()
            .Schedule(BuildAction())
            .ApplyState(BuildState(RoomStateKeys.HeartRate, 80))
            .Play(1_000);

        Assert.Single(room.ScheduledActions);
        Assert.Equal(80, room.State.Entries[RoomStateKeys.HeartRate].Number);
    }

    [Fact]
    public void Stop_StopsTransportButKeepsSchedule()
    {
        // 予約は相対時間なので、再生し直せば同じ位置で発火する。停止では取り消さない。
        var action = BuildAction();

        var room = NewRoom().Schedule(action).Play(1_000).Stop();

        Assert.False(room.Transport.Playing);
        Assert.Same(action, Assert.Single(room.ScheduledActions));
    }

    [Fact]
    public void Schedule_AppendsActions()
    {
        var first = BuildAction("00:00:05:00");
        var second = BuildAction("00:00:09:00");

        var room = NewRoom().Schedule(first).Schedule(second);

        Assert.Equal(new[] { first, second }, room.ScheduledActions);
    }

    [Fact]
    public void Schedule_Null_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => NewRoom().Schedule(null!));
    }

    [Fact]
    public void CancelSchedule_RemovesAction()
    {
        var room = NewRoom().Schedule(BuildAction()).CancelSchedule();

        Assert.Empty(room.ScheduledActions);
    }

    [Fact]
    public void CancelSchedule_WithoutSchedule_ReturnsSameInstance()
    {
        var room = NewRoom();

        Assert.Same(room, room.CancelSchedule());
    }

    [Fact]
    public void ApplyState_MergesPatchIntoCurrentState()
    {
        var room = NewRoom()
            .ApplyState(BuildState(RoomStateKeys.Volume, 30))
            .ApplyState(BuildState(RoomStateKeys.HeartRate, 82));

        Assert.Equal(30, room.State.Entries[RoomStateKeys.Volume].Number);
        Assert.Equal(82, room.State.Entries[RoomStateKeys.HeartRate].Number);
    }

    [Fact]
    public void Play_DoesNotMutateOriginalInstance()
    {
        // Room は不変。更新メソッドは新しいインスタンスを返し、元のインスタンスは変わらない。
        var room = NewRoom();

        room.Play(1_000);

        Assert.False(room.Transport.Playing);
    }
}
