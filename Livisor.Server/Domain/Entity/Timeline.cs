using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Domain.Entity;

// 時刻付きアクション列の集約。「空でない」ことを不変条件とする。
public sealed class Timeline
{
    public IReadOnlyList<TimelineItem> Items { get; }

    private Timeline(IReadOnlyList<TimelineItem> items) => Items = items;

    public static Timeline Create(IReadOnlyList<TimelineItem> items)
    {
        if (items is null || items.Count == 0)
            throw new DomainException("timeline must contain at least one action.");

        return new Timeline(items);
    }
}
