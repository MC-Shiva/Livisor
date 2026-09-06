using Livisor.Server.Domain.Time;

namespace Livisor.Server.Infrastructure;

// IClock の実装。OS の実時間（UTC）をそのまま返す。
// この時刻は「サーバー内部での経過」を測るためだけに使う。クライアントの時計とは比べないので、
// 両者の時計が合っている必要はない（TransportState の式を参照）。
public sealed class SystemClock : IClock
{
    public long UtcNowUnixMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
