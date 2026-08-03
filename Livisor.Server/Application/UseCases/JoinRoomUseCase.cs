using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;

namespace Livisor.Server.Application.UseCases;

// room 参加時のユースケース。
// 蓄積済みのタイムライン一覧を返す（遅延参加者への再送に使う）。なければ空リスト。
public sealed class JoinRoomUseCase
{
    private readonly ITimelineCache _cache;

    public JoinRoomUseCase(ITimelineCache cache) => _cache = cache;

    public IReadOnlyList<Timeline> Join(string roomId) => _cache.GetAll(roomId);
}
