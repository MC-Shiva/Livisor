using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Domain.Entity;

// タイムライン配信の集約ルート。識別子(RoomId)を持ち、現在配信中のタイムラインを保持する。
// BroadcastTimelineAsync の1回の呼び出しは「今の演目を丸ごと差し替える」イベントであり、
// 過去の演目は意味を持たないため、履歴ではなく最新の1件のみを持つ。
// 不変にして更新のたびに新インスタンスを返す設計にすることで、並行アクセス時のロックを
// Domain に持ち込まず、Infrastructure 側（ConcurrentDictionary.AddOrUpdate の再試行）に任せる。
public sealed class Room
{
    public RoomId Id { get; }

    // 現在配信中のタイムライン。まだ何も配信されていなければ null。
    // TODO: 次のタイムラインを配信させることがあるかもしれないので配信中のものはcurrentに
    public Timeline? Current { get; }

    private Room(RoomId id, Timeline? current)
    {
        Id = id;
        Current = current;
    }

    // Current が未設定の Room を作る。
    public static Room Create(RoomId id)
    {
        if (id is null)
            throw new DomainException("roomId must not be null.");

        return new Room(id, null);
    }

    // Current を差し替えた新しい Room を返す。
    public Room SetCurrent(Timeline timeline)
    {
        if (timeline is null)
            throw new DomainException("timeline must not be null.");

        return new Room(Id, timeline);
    }
}
