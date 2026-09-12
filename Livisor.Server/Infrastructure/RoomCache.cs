using System.Collections.Concurrent;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Infrastructure;

// IRoomCache の実装（room ごとの Room 集約をメモリに保持）。
// StreamingHub はコネクションごとに生成され、Unary サービスは呼び出しごとに生成されるため、
// Singleton として共有する。
public sealed class RoomCache : IRoomCache
{
    private readonly ConcurrentDictionary<RoomId, Room> _rooms = new();
    private readonly ScheduledAction[] _defaults;

    public RoomCache(params ScheduledAction[] defaults) => _defaults = defaults.ToArray();

    public Room Get(RoomId roomId)
        => _rooms.TryGetValue(roomId, out var room) ? room : Room.Create(roomId, _defaults);

    // AddOrUpdate は競合時に更新デリゲートを再試行する。Room は不変なので、
    // 再試行しても常に最新の Room を入力にやり直すだけで、更新が失われることはない。
    public Room Update(RoomId roomId, Func<Room, Room> update)
        => _rooms.AddOrUpdate(
            roomId,
            id => update(Room.Create(id, _defaults)),
            (_, existing) => update(existing));

    public void Remove(RoomId roomId)
    {
        _rooms.TryRemove(roomId, out _);
    }
}
