using Grpc.Core;
using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain;
using Livisor.Server.Domain.Time;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Logging;
using Livisor.Server.Presentation.Mapping;
using Livisor.Server.Presentation.Providers;
using Livisor.Shared.DTO;
using Livisor.Shared.Hubs;
using MagicOnion;
using MagicOnion.Server.Hubs;

namespace Livisor.Server.Presentation.Hubs;

// Presentation 層: 状態同期用の StreamingHub。
// 心拍数や音量のように、リアルタイムに更新され続ける値を room 単位で共有する。
// 一度きりの操作（再生・停止・予約）は ITimelineService(Unary) が受ける。
// クライアントとの通信境界として、リクエストの受け口・DTO↔Domain 変換・通信エラー変換を担う。
// 業務ルール(不変条件)は Domain、ユースケース調停は Application に委譲する。
[HubLoggingFilter]
public class RoomStateHub : StreamingHubBase<IRoomStateHub, IRoomStateHubReceiver>, IRoomStateHub, IRoomScopedHub
{
    private readonly RoomUseCase _room;
    private readonly RoomGroupProvider _groups;
    private readonly IClock _clock;
    private readonly ILogger<RoomStateHub> _logger;

    private RoomId? _roomId; // 未参加なら null

    // HubLoggingFilter がログスコープに使うために公開する。
    RoomId? IRoomScopedHub.RoomId => _roomId;

    public RoomStateHub(
        RoomUseCase room,
        RoomGroupProvider groups,
        IClock clock,
        ILogger<RoomStateHub> logger)
    {
        _room = room;
        _groups = groups;
        _clock = clock;
        _logger = logger;
    }

    // room に参加し、参加時点の状態を返す。
    public ValueTask<RoomStatePatch> JoinAsync(string roomId)
    {
        RoomId id;
        try
        {
            id = RoomId.Create(roomId);
        }
        catch (DomainException ex)
        {
            // 不正な roomId 自体はフィルタのスコープに乗らないため、試行値を明示的に載せる。
            // 乗り換え（2 回目以降の JoinAsync）では、スコープの RoomId は乗り換え前のものになる。
            // 拒否された room ではないことに注意する。
            _logger.LogWarn("rejected join: invalid room. ", ("AttemptedRoomId", roomId), ("Reason", ex.Message));
            throw new ReturnStatusException(StatusCode.InvalidArgument, ex.Message);
        }

        // 別 room へ乗り換えるときは、先に元の room から外す。
        // 外さないと切断後も元 room の配信が届き続ける（IRoomStateHub に離脱メソッドが無いため、
        // 乗り換えの唯一の経路が 2 回目の JoinAsync になる）。
        if (_roomId is not null && !_roomId.Equals(id))
            _groups.Leave(_roomId, ConnectionId);

        _roomId = id;

        // 参加と、参加時点の状態の読み取りを同じ区間で行う。
        // 参加直後のクライアントは現在値を知らないため、いまの全項目をこの接続にだけ返す。
        var state = _groups.Join(
            id,
            ConnectionId,
            Client,
            () => RoomStateMapper.ToDto(_room.Get(id).State, _clock.UtcNowUnixMs));
        // フィルタは next() 呼び出し前に RoomId を読むため、この呼び出し内で確定した RoomId 自体は
        // フィルタのスコープに乗らない。RoomId だけ明示的に載せる。
        _logger.LogInfo("joined room. ", ("RoomId", id.Value));

        return new ValueTask<RoomStatePatch>(state);
    }

    // 変化した項目を同じ room の全員へ反映する。
    public ValueTask PublishAsync(RoomStateEntry[] entries)
    {
        if (_roomId is null)
        {
            _logger.LogWarn("rejected publish: not joined. ");
            throw new ReturnStatusException(StatusCode.FailedPrecondition, "join a room before publishing.");
        }

        var roomId = _roomId; // ラムダで捕捉するためローカルへ退避する

        RoomState patch;
        try
        {
            patch = RoomStateMapper.ToDomain(entries);
        }
        catch (DomainException ex)
        {
            // RoomId は前回の JoinAsync 呼び出しで確定済みなので、フィルタのスコープから自動で乗る。
            _logger.LogWarn("rejected publish: invalid state. ", ("Reason", ex.Message));
            throw new ReturnStatusException(StatusCode.InvalidArgument, ex.Message);
        }

        // 確定と配信を同じ区間で行い、キャッシュの確定順と配信順を一致させる。
        // 送信者にも返すことで、全員がサーバーの確定値だけを見る形に揃える。
        var dto = _groups.PublishState(
            roomId,
            () =>
            {
                _room.ApplyState(roomId, patch);
                return RoomStateMapper.ToDto(patch, _clock.UtcNowUnixMs);
            });

        _logger.LogInfo("published state. ", ("EntryCount", dto.Entries.Length));
        return default;
    }

    protected override ValueTask OnDisconnected()
    {
        if (_roomId is not null)
            _groups.Leave(_roomId, ConnectionId);

        _logger.LogInfo("disconnected. ", ("RoomId", _roomId?.Value), ("ConnectionId", ConnectionId));
        return default;
    }
}
