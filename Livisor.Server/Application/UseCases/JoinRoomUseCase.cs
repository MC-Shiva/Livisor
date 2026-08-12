using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Application.UseCases;

// room 参加時のユースケース。
// 現在配信中のタイムラインを返す（遅延参加者への再送に使う）。なければ null。
public sealed class JoinRoomUseCase
{
    private readonly IRoomCache _cache;

    public JoinRoomUseCase(IRoomCache cache) => _cache = cache;

    public Timeline? Join(RoomId roomId) => _cache.Get(roomId).Current;
}
