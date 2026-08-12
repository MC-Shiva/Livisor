using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Infrastructure;
using Livisor.Shared.Common;
using Xunit.Abstractions;

namespace Livisor.Server.Tests.Infrastructure;

public class RoomCacheTests(ITestOutputHelper output)
{
    private readonly RoomCache _cache = new();

    private static Timeline BuildTimeline(string time = "10:00:00:00", ActionType action = ActionType.Start)
    {
        return Timeline.Create([new TimelineItem(PlaybackTime.Parse(time), action, 1)]);
    }

    [Fact]
    public void RoomCache_BasicOperations()
    {
        var room1 = RoomId.Create("room1");
        var room2 = RoomId.Create("room2");
        var t1 = BuildTimeline("10:00:00:00");
        var t2 = BuildTimeline("11:00:00:00");

        // 存在しない room は Current が未設定
        Assert.Null(_cache.Get(room1).Current);

        // SetCurrentTimeline すると Current がそれになる
        _cache.SetCurrentTimeline(room1, t1);
        Assert.Same(t1, _cache.Get(room1).Current);

        // 再度 SetCurrentTimeline すると差し替わる（前の値は残らない）
        _cache.SetCurrentTimeline(room1, t2);
        Assert.Same(t2, _cache.Get(room1).Current);

        // 別の room は独立している
        _cache.SetCurrentTimeline(room2, t1);
        Assert.Same(t1, _cache.Get(room2).Current);

        // Remove で該当 room のみ削除される
        _cache.Remove(room1);
        Assert.Null(_cache.Get(room1).Current);
        Assert.Same(t1, _cache.Get(room2).Current);
    }

    [Fact]
    public void SetCurrentTimeline_ConcurrentCallsToSameRoom_NoExceptionAndLastWriteWins()
    {
        // Room は単一スロットの不変オブジェクト。同時に差し替えても例外を起こさず、
        // どれか1つの呼び出し結果が最終的に残ることを確認する（要素の欠落という概念自体がない）。
        const int concurrency = 100;
        var roomId = RoomId.Create("room-concurrent");
        var timelines = Enumerable.Range(0, concurrency)
            .Select(i => BuildTimeline($"{i % 24:D2}:00:00:00"))
            .ToArray();

        Parallel.For(0, concurrency, i => _cache.SetCurrentTimeline(roomId, timelines[i]));

        var current = _cache.Get(roomId).Current;
        output.WriteLine($"current is one of the {concurrency} written timelines: {current is not null && timelines.Contains(current)}");
        Assert.NotNull(current);
        Assert.Contains(current, timelines);
    }
}
