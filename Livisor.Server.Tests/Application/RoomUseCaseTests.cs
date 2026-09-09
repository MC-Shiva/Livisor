using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.Time;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;
using NSubstitute;

namespace Livisor.Server.Tests.Application;

public class RoomUseCaseTests
{
    private static readonly RoomId RoomId = RoomId.Create("room1");

    // IRoomCache.Update は「現在の Room に更新関数を当てて返す」だけを模した振る舞いにする。
    private static IRoomCache StubCache(Room current)
    {
        var cache = Substitute.For<IRoomCache>();
        cache.Update(RoomId, Arg.Any<Func<Room, Room>>())
            .Returns(call => call.Arg<Func<Room, Room>>()!(current));
        return cache;
    }

    private static IClock StubClock(long nowUnixMs)
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNowUnixMs.Returns(nowUnixMs);
        return clock;
    }

    private static RoomUseCase Create(Room current, long nowUnixMs = 1_000)
        => new(StubCache(current), StubClock(nowUnixMs));

    private static ScheduledAction BuildAction(string offset = "00:00:05:00")
        => new(PlaybackTime.Parse(offset), ActionType.VolumeChange, 10);

    private static RoomState BuildState(string key, ActionValue value)
        => RoomState.Create([new KeyValuePair<string, ActionValue>(key, value)]);

    [Fact]
    public void Get_ReturnsRoomFromCache()
    {
        var expected = Room.Create(RoomId).Play(1_000);
        var cache = Substitute.For<IRoomCache>();
        cache.Get(RoomId).Returns(expected);

        var room = new RoomUseCase(cache, StubClock(2_000)).Get(RoomId);

        Assert.Same(expected, room);
    }

    [Fact]
    public void Play_UsesClockAsStartTime()
    {
        var room = Create(Room.Create(RoomId), 1_700_000_000_000).Play(RoomId);

        Assert.True(room.Transport.Playing);
        Assert.Equal(1_700_000_000_000, room.Transport.StartedAtUnixMs);
    }

    [Fact]
    public void Play_ReadsClockOnlyOnce()
    {
        // 更新関数は競合時に再試行されうるため、時刻はその外側で1回だけ確定させる。
        var clock = StubClock(1_000);
        var useCase = new RoomUseCase(StubCache(Room.Create(RoomId)), clock);

        useCase.Play(RoomId);

        _ = clock.Received(1).UtcNowUnixMs;
    }

    [Fact]
    public void Play_CallsCacheUpdateOnce()
    {
        var cache = StubCache(Room.Create(RoomId));

        new RoomUseCase(cache, StubClock(1_000)).Play(RoomId);

        cache.Received(1).Update(RoomId, Arg.Any<Func<Room, Room>>());
    }

    [Fact]
    public void Stop_StopsTransport()
    {
        var room = Create(Room.Create(RoomId).Play(1_000), 2_000).Stop(RoomId);

        Assert.False(room.Transport.Playing);
        Assert.Null(room.Transport.StartedAtUnixMs);
    }

    [Fact]
    public void Schedule_StoresActionOnRoom()
    {
        var cache = StubCache(Room.Create(RoomId));
        var action = BuildAction();

        var room = new RoomUseCase(cache, StubClock(1_000)).Schedule(RoomId, action);

        Assert.Same(action, room.Scheduled);
        cache.Received(1).Update(RoomId, Arg.Any<Func<Room, Room>>());
    }

    [Fact]
    public void Schedule_ReplacesExistingAction()
    {
        // キューは最大1件。既存の予約は置き換える。
        var replacement = BuildAction("00:00:09:00");

        var room = Create(Room.Create(RoomId).Schedule(BuildAction("00:00:05:00"))).Schedule(RoomId, replacement);

        Assert.Same(replacement, room.Scheduled);
    }

    [Fact]
    public void Schedule_KeepsTransport()
    {
        // 予約の登録は再生状態を動かさない。
        var room = Create(Room.Create(RoomId).Play(1_000), 2_000).Schedule(RoomId, BuildAction());

        Assert.True(room.Transport.Playing);
        Assert.Equal(1_000, room.Transport.StartedAtUnixMs);
    }

    [Fact]
    public void CancelSchedule_RemovesAction()
    {
        var room = Create(Room.Create(RoomId).Schedule(BuildAction())).CancelSchedule(RoomId);

        Assert.Null(room.Scheduled);
    }

    [Fact]
    public void ApplyState_MergesPatchIntoRoomState()
    {
        var current = Room.Create(RoomId).ApplyState(BuildState(RoomStateKeys.Volume, 30));
        var cache = StubCache(current);

        var room = new RoomUseCase(cache, StubClock(1_000)).ApplyState(RoomId, BuildState(RoomStateKeys.HeartRate, 82));

        // 差分に無い項目は元の値を保つ。
        Assert.Equal(30, room.State.Entries[RoomStateKeys.Volume].Number);
        Assert.Equal(82, room.State.Entries[RoomStateKeys.HeartRate].Number);
        cache.Received(1).Update(RoomId, Arg.Any<Func<Room, Room>>());
    }

    [Fact]
    public void ApplyState_SameKeyTwice_KeepsLatestValue()
    {
        var current = Room.Create(RoomId).ApplyState(BuildState(RoomStateKeys.HeartRate, 70));

        var room = Create(current).ApplyState(RoomId, BuildState(RoomStateKeys.HeartRate, 95));

        Assert.Equal(95, room.State.Entries[RoomStateKeys.HeartRate].Number);
    }
}
