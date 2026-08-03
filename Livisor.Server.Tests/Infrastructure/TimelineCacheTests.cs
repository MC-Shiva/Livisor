using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Infrastructure;
using Livisor.Shared.DTO;
using Xunit.Abstractions;

namespace Livisor.Server.Tests.Infrastructure;

public class TimelineCacheTests(ITestOutputHelper output)
{
    private readonly TimelineCache _cache = new();

    private static Timeline BuildTimeline(string time = "10:00:00:00", ActionType action = ActionType.Start)
    {
        return Timeline.Create([new TimelineItem(PlaybackTime.Parse(time), action, 1)]);
    }

    [Fact]
    public void TimelineCache_BasicOperations()
    {
        var t1 = BuildTimeline("10:00:00:00");
        var t2 = BuildTimeline("11:00:00:00");

        // 存在しない room は空リスト
        Assert.Empty(_cache.GetAll("room1"));

        // 追加した順に全件取得できる
        _cache.Add("room1", t1);
        _cache.Add("room1", t2);
        var result = _cache.GetAll("room1");
        Assert.Equal(2, result.Count);
        Assert.Same(t1, result[0]);
        Assert.Same(t2, result[1]);

        // 別の room は独立している
        _cache.Add("room2", t1);
        Assert.Single(_cache.GetAll("room2"));

        // RemoveAll で該当 room のみ削除される
        _cache.RemoveAll("room1");
        Assert.Empty(_cache.GetAll("room1"));
        Assert.Single(_cache.GetAll("room2"));
    }
}
