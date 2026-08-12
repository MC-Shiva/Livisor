using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Application.UseCases;

// タイムライン配信時のユースケース。
// 検証は Domain(Timeline/PlaybackTime)の生成時点で完了している前提で、
// ここでは「room の現在配信中タイムラインを差し替える」アプリケーションの流れを担う。
// （配信対象への実際のプッシュは接続に紐づく Presentation 層で行う）
public sealed class BroadcastTimelineUseCase
{
    private readonly IRoomCache _cache;

    public BroadcastTimelineUseCase(IRoomCache cache) => _cache = cache;

    public void Broadcast(RoomId roomId, Timeline timeline) => _cache.SetCurrentTimeline(roomId, timeline);
}
