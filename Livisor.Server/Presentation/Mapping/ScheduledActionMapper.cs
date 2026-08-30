using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.DTO;

namespace Livisor.Server.Presentation.Mapping;

// Shared.DTO ↔ Domain の相互変換（予約アクション）。DTO 依存を Presentation 境界に閉じ込める。
public static class ScheduledActionMapper
{
    // 受信 DTO → ドメイン。不正な time はここで DomainException となる。
    public static ScheduledAction ToDomain(TimelineAction action)
    {
        if (action is null)
            throw new DomainException("action must not be null.");

        return new ScheduledAction(PlaybackTimeParser.Parse(action.Time), action.Action, action.Value);
    }

    // ドメイン → 応答 DTO。
    public static TimelineAction ToDto(ScheduledAction action) => new()
    {
        Time = action.Offset.ToRawString(),
        Action = action.Action,
        Value = action.Value,
    };
}
