using System.Collections.Concurrent;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;

namespace Livisor.Server.Infrastructure;

// ITimelineCache の実装（room ごとのタイムライン履歴をメモリに保持）。
// StreamingHub はコネクションごとに生成されるため、Singleton として共有する。
public sealed class TimelineCache : ITimelineCache
{
    private readonly ConcurrentDictionary<string, List<Timeline>> _timelines = new();

    public void Add(string roomId, Timeline timeline)
    {
        _timelines.GetOrAdd(roomId, _ => []).Add(timeline);
    }

    public IReadOnlyList<Timeline> GetAll(string roomId)
        => _timelines.TryGetValue(roomId, out var list) ? list.ToList() : [];

    public void RemoveAll(string roomId)
    {
        _timelines.TryRemove(roomId, out _);
    }
}
