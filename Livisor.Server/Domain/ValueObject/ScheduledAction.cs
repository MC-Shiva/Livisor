using Livisor.Shared.Common;

namespace Livisor.Server.Domain.ValueObject;

// 予約アクション（検証済み）。Issue #17 の決定により、room ごとに最大 1 件だけ持つ。
// Offset は絶対時刻ではなく再生開始からの相対時間なので、再生をやり直しても同じ位置で発火する。
// ActionType は「操作種別の共有語彙」としてワイヤ契約(Shared)のものを再利用する。
public sealed class ScheduledAction
{
    public PlaybackTime Offset { get; }
    public ActionType Action { get; }
    public ActionValue Value { get; }

    public ScheduledAction(PlaybackTime offset, ActionType action, ActionValue value)
    {
        // 「検証済み」であることをこの型で保証する。操作と値の種類が合わない組はここで弾く。
        // 通さないと、サーバーが不正な予約を正常応答として配り、解釈がクライアントごとに分かれる。
        var expected = action switch
        {
            ActionType.Play => ActionValueKind.Bool,
            ActionType.VolumeChange => ActionValueKind.Number,
            _ => throw new DomainException($"unknown action type: {action}."),
        };

        if (value.Kind != expected)
            throw new DomainException($"action '{action}' must have a {expected} value, but was {value.Kind}.");

        Offset = offset;
        Action = action;
        Value = value;
    }

    // 再生開始時刻を基準にした発火予定のサーバー時刻(UTC ミリ秒)。
    // センチ秒の整数から求めることで、秒の小数を経由する丸め誤差を避ける。
    // サーバーはこの時刻で発火しない。発火はクライアントが行う（2026-08-29 の決定 / Issue #19 のコメント）。
    // 伝送ジッタがそのまま実行時刻のずれになるため、サーバー側スケジューラは持たない。
    // 本番の呼び出し元は無く、クライアントが持つ式と同じ計算をテストで固定するために置く。
    public long FireAtUnixMs(long startedAtUnixMs) => startedAtUnixMs + Offset.TotalCentiseconds * 10L;
}
