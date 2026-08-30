using Livisor.Server.Domain.Time;

namespace Livisor.Server.Infrastructure;

// IClock の実装。OS の実時間（UTC）をそのまま返す。
// サーバー側の時刻精度は NTP による OS の同期に委ねる。クライアント側も NTP で合っている前提を置き、
// 残るクロック差は ITimelineService.GetServerTimeAsync の測定値でクライアントが補正する。
public sealed class SystemClock : IClock
{
    public long UtcNowUnixMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
