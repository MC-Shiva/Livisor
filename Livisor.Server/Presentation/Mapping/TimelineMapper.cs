using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;

namespace Livisor.Server.Presentation.Mapping;

// Shared.DTO ↔ Domain の相互変換。DTO 依存を Presentation 境界に閉じ込める。
public static class TimelineMapper
{
    // 受信 DTO → ドメイン。不正な time / 空配列はここで DomainException となる。
    public static Timeline ToDomain(TimelineAction[] actions)
    {
        if (actions is null)
            throw new DomainException("timeline must not be null.");

        var items = new List<TimelineItem>(actions.Length);
        foreach (var action in actions)
        {
            if (action is null)
                throw new DomainException("action must not be null.");

            items.Add(new TimelineItem(PlaybackTimeParser.Parse(action.Time), action.Action, action.Value));
        }

        return Timeline.Create(items);
    }

    // ドメイン → Request DTO。
    public static TimelineAction[] ToDto(Timeline timeline)
    {
        var result = new TimelineAction[timeline.Items.Count];
        for (var i = 0; i < timeline.Items.Count; i++)
        {
            var item = timeline.Items[i];
            result[i] = new TimelineAction
            {
                Time = item.Time.ToRawString(),
                Action = item.Action,
                Value = item.Value,
            };
        }

        return result;
    }
}
