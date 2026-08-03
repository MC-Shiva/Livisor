using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;

namespace Livisor.Server.Application.UseCases;

// タイムライン配信時のユースケース。
// 検証は Domain(Timeline/PlaybackTime)の生成時点で完了している前提で、
// ここでは「タイムラインをキャッシュに蓄積する」アプリケーションの流れを担う。
// （配信対象への実際のプッシュは接続に紐づく Presentation 層で行う）
public sealed class BroadcastTimelineUseCase
{
    private readonly ITimelineCache _cache;

    public BroadcastTimelineUseCase(ITimelineCache cache) => _cache = cache;

    public void Broadcast(string roomId, Timeline timeline) => _cache.Add(roomId, timeline);
}
