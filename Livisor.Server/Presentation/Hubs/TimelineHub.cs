using Grpc.Core;
using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Presentation.Mapping;
using Livisor.Shared.DTO;
using Livisor.Shared.Hubs;
using MagicOnion;
using MagicOnion.Server.Hubs;

namespace Livisor.Server.Presentation.Hubs;

// Presentation 層: タイムライン配信用の StreamingHub。
// クライアントとの通信境界として、リクエストの受け口・DTO↔Domain 変換・通信エラー変換を担う。
// 業務ルール(不変条件)は Domain、ユースケース調停は Application に委譲する。
public class TimelineHub : StreamingHubBase<ITimelineHub, ITimelineHubReceiver>, ITimelineHub
{
    private readonly JoinRoomUseCase _joinRoom;
    private readonly BroadcastTimelineUseCase _broadcast;

    private IGroup<ITimelineHubReceiver>? _group;
    private string _roomId = string.Empty;

    public TimelineHub(JoinRoomUseCase joinRoom, BroadcastTimelineUseCase broadcast)
    {
        _joinRoom = joinRoom;
        _broadcast = broadcast;
    }

    // room（グループ）に参加する。
    public async ValueTask JoinAsync(string roomId)
    {
        _roomId = roomId;
        _group = await Group.AddAsync(roomId);

        // 遅延参加対応: 蓄積済みのタイムライン一覧を参加者にだけ順に再送する。現在時刻を基準とするため即時再生される。
        var existing = _joinRoom.Join(roomId);
        foreach (var t in existing)
        {
            _group.Single(ConnectionId).OnBroadcastTimeline(TimelineMapper.ToDto(t), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
    }

    // タイムラインを配信する。
    public ValueTask BroadcastTimelineAsync(TimelineAction[] actions)
    {
        Timeline timeline;
        try
        {
            timeline = TimelineMapper.ToDomain(actions);
        }
        catch (DomainException ex)
        {
            throw new ReturnStatusException(StatusCode.InvalidArgument, ex.Message);
        }

        _broadcast.Broadcast(_roomId, timeline);

        // 送信時刻を全受信者の再生基準にすることで、同一 room 内の同時発火を保証する。
        var broadcastAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _group?.Except([ConnectionId]).OnBroadcastTimeline(actions, broadcastAtMs);
        return default;
    }

    protected override ValueTask OnDisconnected() => default;
}
