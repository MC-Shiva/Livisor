using System.Collections.Concurrent;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Infrastructure;

// IRoomCache の実装（room ごとの Room 集約をメモリに保持）。
// StreamingHub はコネクションごとに生成されるため、Singleton として共有する。
public sealed class RoomCache : IRoomCache
{
    private readonly ConcurrentDictionary<RoomId, Room> _rooms = new();

    // AddOrUpdate は競合時に更新デリゲートを再試行する。Room は不変な単一スロットの
    // 差し替えのため、同時呼び出しは last-write-wins（どちらかが残る）で、例外や
    // 状態破壊は起きない。
    public Room SetCurrentTimeline(RoomId roomId, Timeline timeline)
        => _rooms.AddOrUpdate(
            roomId,
            id => Room.Create(id).SetCurrent(timeline),
            (_, existing) => existing.SetCurrent(timeline));

    public Room Get(RoomId roomId)
        => _rooms.TryGetValue(roomId, out var room) ? room : Room.Create(roomId);

    public void Remove(RoomId roomId)
    {
        _rooms.TryRemove(roomId, out _);
    }
}
