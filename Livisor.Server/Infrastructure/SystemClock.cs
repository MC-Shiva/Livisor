using Livisor.Server.Domain.Time;

namespace Livisor.Server.Infrastructure;

// IClock の実装。OS の実時間（UTC）をそのまま返す。
// サーバー側の時刻精度は NTP による OS の同期に委ね、クライアントとのクロック差は
// ITimelineService.GetServerTimeAsync の測定値でクライアント側が補正する。
public sealed class SystemClock : IClock
{
    public long UtcNowUnixMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
