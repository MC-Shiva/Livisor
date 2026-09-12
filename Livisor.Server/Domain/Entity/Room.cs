using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Domain.Entity;

// room ごとの再生状態・デフォルト演出・追加予約・同期値を保持する。
// 更新は新しい Room を返し、RoomCache の並行更新で元の状態を書き換えない。
public sealed class Room
{
    public RoomId Id { get; }
    public Transport Transport { get; }
    public IReadOnlyList<ScheduledAction> DefaultActions { get; }
    public IReadOnlyList<ScheduledAction> ScheduledActions { get; }
    public RoomState State { get; }

    private Room(RoomId id, Transport transport, IReadOnlyList<ScheduledAction> defaults,
        IReadOnlyList<ScheduledAction> scheduled, RoomState state)
    {
        Id = id;
        Transport = transport;
        DefaultActions = defaults;
        ScheduledActions = scheduled;
        State = state;
    }

    public static Room Create(RoomId id, params ScheduledAction[] defaults)
    {
        if (id is null)
            throw new DomainException("roomId must not be null.");
        ValidateActions(defaults);
        return new Room(id, Transport.Stopped, Array.AsReadOnly(defaults.ToArray()),
            Array.Empty<ScheduledAction>(), RoomState.Empty);
    }

    public Room Play(long startedAtUnixMs)
        => new(Id, Transport.Start(startedAtUnixMs), DefaultActions, ScheduledActions, State);

    public Room Stop() => new(Id, Transport.Stop(), DefaultActions, ScheduledActions, State);

    // 既存の予約を残して追加する。入力配列は保持せず、Room 内では読み取り専用にする。
    public Room Schedule(params ScheduledAction[] actions)
    {
        ValidateActions(actions);
        return new Room(Id, Transport, DefaultActions,
            Array.AsReadOnly(ScheduledActions.Concat(actions).ToArray()), State);
    }

    // Admin が追加した予約だけを取り消す。デフォルト演出は残す。
    public Room CancelSchedule() => ScheduledActions.Count == 0 ? this
        : new Room(Id, Transport, DefaultActions, Array.Empty<ScheduledAction>(), State);

    public Room ApplyState(RoomState patch)
        => new(Id, Transport, DefaultActions, ScheduledActions, State.Merge(patch));

    private static void ValidateActions(ScheduledAction[] actions)
    {
        if (actions is null || actions.Any(action => action is null))
            throw new DomainException("scheduled actions must not be null or contain null.");
    }
}
