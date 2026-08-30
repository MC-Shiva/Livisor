namespace Livisor.Server.Domain.ValueObject;

// 再生トランスポート。再生中かどうかと、再生を開始したサーバー時刻(UTC ミリ秒)を持つ。
// 予約アクションはこの開始時刻からの相対時間で発火するため、開始時刻は「いつ発火するか」を
// 決める基準になる。停止すると基準を失うので開始時刻も落とす。
// 識別子を持たず中身だけが意味を持つため ValueObject に置く。
// ただし Equals/GetHashCode は実装していないので、比較は参照同一性になる。
// 同じ扱いの RoomState / ScheduledAction も等価性は未実装。値で比べたくなったら先に実装する。
public sealed class Transport
{
    public bool Playing { get; }

    // 再生を開始したサーバー時刻(UTC ミリ秒)。停止中は null。
    public long? StartedAtUnixMs { get; }

    private Transport(bool playing, long? startedAtUnixMs)
    {
        Playing = playing;
        StartedAtUnixMs = startedAtUnixMs;
    }

    public static Transport Stopped { get; } = new(false, null);

    // 再生を開始する。すでに再生中なら開始時刻を動かさない（渡した値は無視される）。
    // 動かすと予約アクションの発火位置がずれるため、再生中の再呼び出しは無視する。
    // 停止してから開始し直した場合は新しい開始時刻になり、再生位置は 0 に戻る。
    public Transport Start(long startedAtUnixMs)
    {
        if (startedAtUnixMs < 0)
            throw new DomainException("server time must not be negative.");

        return Playing ? this : new Transport(true, startedAtUnixMs);
    }

    public Transport Stop() => Playing ? Stopped : this;

    // 再生開始からの経過ミリ秒。停止中と、開始時刻より前の時刻を渡された場合は 0。
    // 現状の呼び出し元はテストのみ。再生位置はクライアントが ServerTimeMs - StartedAtServerMs で求める。
    public long PositionMs(long nowUnixMs)
        => Playing && StartedAtUnixMs is { } startedAt && nowUnixMs > startedAt
            ? nowUnixMs - startedAt
            : 0;
}
