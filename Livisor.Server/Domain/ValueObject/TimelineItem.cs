using Livisor.Shared.DTO;

namespace Livisor.Server.Domain.ValueObject;

// タイムライン上の 1 アクション（検証済み）。
// ActionType は「操作種別の共有語彙」としてワイヤ契約(Shared.DTO)のものを再利用する。
public sealed class TimelineItem
{
    public PlaybackTime Time { get; }
    public ActionType Action { get; }
    public int Value { get; }

    public TimelineItem(PlaybackTime time, ActionType action, int value)
    {
        Time = time;
        Action = action;
        Value = value;
    }
}
