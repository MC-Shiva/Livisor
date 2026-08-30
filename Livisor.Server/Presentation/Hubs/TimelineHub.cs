using Grpc.Core;
using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Logging;
using Livisor.Server.Presentation.Mapping;
using Livisor.Shared.DTO;
using Livisor.Shared.Hubs;
using MagicOnion;
using MagicOnion.Server.Hubs;

namespace Livisor.Server.Presentation.Hubs;

// Presentation 層: タイムライン配信用の StreamingHub。
// クライアントとの通信境界として、リクエストの受け口・DTO↔Domain 変換・通信エラー変換を担う。
// 業務ルール(不変条件)は Domain、ユースケース調停は Application に委譲する。
[HubLoggingFilter]
public class TimelineHub : StreamingHubBase<ITimelineHub, ITimelineHubReceiver>, ITimelineHub, IRoomScopedHub
{
    private readonly JoinRoomUseCase _joinRoom;
    private readonly BroadcastTimelineUseCase _broadcast;
    private readonly ILogger<TimelineHub> _logger;

    private IGroup<ITimelineHubReceiver>? _group;
    private RoomId? _roomId; // 未参加なら null

    // HubLoggingFilter がログスコープに使うために公開する。
    RoomId? IRoomScopedHub.RoomId => _roomId;

    public TimelineHub(JoinRoomUseCase joinRoom, BroadcastTimelineUseCase broadcast, ILogger<TimelineHub> logger)
    {
        _joinRoom = joinRoom;
        _broadcast = broadcast;
        _logger = logger;
    }

    // room（グループ）に参加する。
    public async ValueTask JoinAsync(string roomId)
    {
        RoomId id;
        try
        {
            id = RoomId.Create(roomId);
        }
        catch (DomainException ex)
        {
            // この時点では _roomId が確定していない(フィルタのスコープに乗らない)ため、試行値を明示的に載せる。
            _logger.LogWarn("rejected join: invalid room. ", ("AttemptedRoomId", roomId), ("Reason", ex.Message));
            throw new ReturnStatusException(StatusCode.InvalidArgument, ex.Message);
        }

        _roomId = id;
        _group = await Group.AddAsync(roomId);
        // フィルタは next() 呼び出し前に RoomId を読むため、この呼び出し内で確定した RoomId 自体は
        // フィルタのスコープに乗らない(この行で初めて確定するため)。RoomId だけ明示的に載せる。
        _logger.LogInfo("joined room. ", ("RoomId", id.Value));

        // 遅延参加対応: 現在配信中のタイムラインがあれば参加者にだけ再送する。現在時刻を基準とするため即時再生される。
        var current = _joinRoom.Join(id);
        if (current is not null)
        {
            _logger.LogInfo("resent current timeline. ", ("RoomId", id.Value), ("ActionCount", current.Items.Count));
            _group.Single(ConnectionId).OnBroadcastTimeline(TimelineMapper.ToDto(current), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
    }

    // タイムラインを配信する。
    public ValueTask BroadcastTimelineAsync(TimelineAction[] actions)
    {
        if (_roomId is null)
        {
            _logger.LogWarn("rejected broadcast: not joined. ");
            throw new ReturnStatusException(StatusCode.FailedPrecondition, "join a room before broadcasting.");
        }

        Timeline timeline;
        try
        {
            timeline = TimelineMapper.ToDomain(actions);
        }
        catch (DomainException ex)
        {
            // RoomId は前回の JoinAsync 呼び出しで確定済みなので、フィルタのスコープから自動で乗る。
            _logger.LogWarn("rejected broadcast: invalid timeline. ", ("Reason", ex.Message));
            throw new ReturnStatusException(StatusCode.InvalidArgument, ex.Message);
        }

        _broadcast.Broadcast(_roomId, timeline);

        // 送信時刻を全受信者の再生基準にすることで、同一 room 内の同時発火を保証する。
        var broadcastAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _logger.LogInfo("broadcast timeline. ", ("ActionCount", actions.Length), ("BroadcastAtMs", broadcastAtMs));
        _group?.Except([ConnectionId]).OnBroadcastTimeline(actions, broadcastAtMs);
        return default;
    }

    protected override ValueTask OnDisconnected()
    {
        _logger.LogInfo("disconnected. ", ("RoomId", _roomId?.Value), ("ConnectionId", ConnectionId));
        return default;
    }
}
