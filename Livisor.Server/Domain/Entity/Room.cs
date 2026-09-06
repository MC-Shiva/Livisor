using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Domain.Entity;

// 配信の集約ルート。識別子(RoomId)を持ち、room 単位で次の3つを保持する。
//   - Transport : 再生中かどうかと再生開始時刻。予約アクションの発火基準になる。
//   - Scheduled : 予約アクション。Issue #17 の決定によりキューは最大1件。
//   - State     : 心拍数や音量など、同期し続ける値。
// 不変にして更新のたびに新インスタンスを返す設計にすることで、並行アクセス時のロックを
// Domain に持ち込まず、Infrastructure 側（ConcurrentDictionary.AddOrUpdate の再試行）に任せる。
public sealed class Room
{
    public RoomId Id { get; }

    public Transport Transport { get; }

    // 予約アクション。未予約なら null。
    public ScheduledAction? Scheduled { get; }

    public RoomState State { get; }

    private Room(RoomId id, Transport transport, ScheduledAction? scheduled, RoomState state)
    {
        Id = id;
        Transport = transport;
        Scheduled = scheduled;
        State = state;
    }

    // 停止中・未予約・状態なしの Room を作る。
    public static Room Create(RoomId id)
    {
        if (id is null)
            throw new DomainException("roomId must not be null.");

        return new Room(id, Transport.Stopped, null, RoomState.Empty);
    }

    // 再生を開始した新しい Room を返す。予約と状態は引き継ぐ。
    // すでに再生中なら開始時刻は動かないため、渡した値は無視される。
    public Room Play(long startedAtUnixMs) => new(Id, Transport.Start(startedAtUnixMs), Scheduled, State);

    // 再生を停止した新しい Room を返す。予約は取り消さない（再生し直せば同じ相対位置で発火する）。
    public Room Stop() => new(Id, Transport.Stop(), Scheduled, State);

    // 予約アクションを差し替えた新しい Room を返す。
    public Room Schedule(ScheduledAction action)
    {
        if (action is null)
            throw new DomainException("scheduled action must not be null.");

        return new Room(Id, Transport, action, State);
    }

    // 予約アクションを取り消した新しい Room を返す。
    public Room CancelSchedule() => Scheduled is null ? this : new Room(Id, Transport, null, State);

    // 状態差分を重ねた新しい Room を返す。
    public Room ApplyState(RoomState patch) => new(Id, Transport, Scheduled, State.Merge(patch));
}
