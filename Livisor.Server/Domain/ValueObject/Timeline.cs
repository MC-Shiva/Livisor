namespace Livisor.Server.Domain.ValueObject;

// 時刻付きアクション列。識別子を持たず中身だけで同一性が決まるため ValueObject に置く。
// 「空でないこと」「時刻順であること」を不変条件とする。
public sealed class Timeline
{
    public IReadOnlyList<TimelineItem> Items { get; }

    private Timeline(IReadOnlyList<TimelineItem> items) => Items = items;

    public static Timeline Create(IReadOnlyList<TimelineItem> items)
    {
        if (items is null || items.Count == 0)
            throw new DomainException("timeline must contain at least one action.");

        // 時刻順を不変条件にする。同時刻の複数操作は正当なので許可する（狭義単調増加ではなく広義）。
        for (var i = 1; i < items.Count; i++)
        {
            if (items[i].Time.TotalCentiseconds < items[i - 1].Time.TotalCentiseconds)
                throw new DomainException("timeline must be ordered by time.");
        }

        return new Timeline(items);
    }
}
