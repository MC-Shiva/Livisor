using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Infrastructure;
using Livisor.Shared.Common;
using Xunit.Abstractions;

namespace Livisor.Server.Tests.Infrastructure;

public class RoomCacheTests(ITestOutputHelper output)
{
    private readonly RoomCache _cache = new();

    private static ScheduledAction BuildAction(string offset = "00:00:05:00")
        => new(PlaybackTime.Parse(offset), ActionType.VolumeChange, 10);

    [Fact]
    public void RoomCache_BasicOperations()
    {
        var room1 = RoomId.Create("room1");
        var room2 = RoomId.Create("room2");

        // 存在しない room は初期状態（停止中・未予約）で返る
        Assert.False(_cache.Get(room1).Transport.Playing);
        Assert.Null(_cache.Get(room1).Scheduled);

        // Update すると更新後の Room が保持される
        _cache.Update(room1, room => room.Play(1_000));
        Assert.True(_cache.Get(room1).Transport.Playing);
        Assert.Equal(1_000, _cache.Get(room1).Transport.StartedAtUnixMs);

        // 続けて Update すると前の更新の上に重なる
        var action = BuildAction();
        _cache.Update(room1, room => room.Schedule(action));
        Assert.True(_cache.Get(room1).Transport.Playing);
        Assert.Same(action, _cache.Get(room1).Scheduled);

        // 別の room は独立している
        Assert.False(_cache.Get(room2).Transport.Playing);

        // Remove で該当 room のみ初期状態に戻る
        _cache.Update(room2, room => room.Play(2_000));
        _cache.Remove(room1);
        Assert.False(_cache.Get(room1).Transport.Playing);
        Assert.True(_cache.Get(room2).Transport.Playing);
    }

    [Fact]
    public void Update_ReturnsUpdatedRoom()
    {
        var roomId = RoomId.Create("room1");

        var room = _cache.Update(roomId, r => r.Play(1_000));

        Assert.Equal(1_000, room.Transport.StartedAtUnixMs);
    }

    [Fact]
    public void Update_ConcurrentCallsToSameRoom_NoExceptionAndAllStatePreserved()
    {
        // Room は不変オブジェクト。同時に更新しても例外を起こさず、
        // AddOrUpdate の再試行によってどの更新も失われないことを確認する。
        const int concurrency = 100;
        var roomId = RoomId.Create("room-concurrent");

        Parallel.For(0, concurrency, i =>
            _cache.Update(roomId, room => room.ApplyState(
                RoomState.Create([new KeyValuePair<string, ActionValue>($"key{i}", i)]))));

        var state = _cache.Get(roomId).State;
        output.WriteLine($"merged entries: {state.Entries.Count} / {concurrency}");
        Assert.Equal(concurrency, state.Entries.Count);
    }

    [Fact]
    public void Update_ConcurrentPlayAndStop_LeavesConsistentTransport()
    {
        // 再生と停止のように同じスロットを奪い合う更新は last-write-wins になる。
        // どちらが残っても、再生中なら開始時刻がある・停止中なら無い、という整合は保たれる。
        const int concurrency = 100;
        var roomId = RoomId.Create("room-transport");

        Parallel.For(0, concurrency, i =>
            _cache.Update(roomId, room => i % 2 == 0 ? room.Play(1_000 + i) : room.Stop()));

        var transport = _cache.Get(roomId).Transport;
        output.WriteLine($"playing: {transport.Playing}, startedAt: {transport.StartedAtUnixMs}");
        Assert.Equal(transport.Playing, transport.StartedAtUnixMs is not null);
    }
}
