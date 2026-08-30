using Grpc.Core;
using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.Time;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Logging;
using Livisor.Server.Presentation.Mapping;
using Livisor.Server.Presentation.Providers;
using Livisor.Shared.DTO;
using Livisor.Shared.UnaryServices;
using MagicOnion;
using MagicOnion.Server;

namespace Livisor.Server.Presentation.UnaryServices;

// Presentation 層: タイムライン（再生トランスポートと予約アクション）の Unary サービス。
// 遅延なく確定させたい操作をここで受け、確定した結果を応答で返すと同時に、
// 同じ room の StreamingHub 参加者へも通知する。
// クライアントとの通信境界として、リクエストの受け口・DTO↔Domain 変換・通信エラー変換を担う。
public class TimelineService : ServiceBase<ITimelineService>, ITimelineService
{
    private readonly RoomUseCase _room;
    private readonly RoomGroupProvider _groups;
    private readonly IClock _clock;
    private readonly ILogger<TimelineService> _logger;

    public TimelineService(
        RoomUseCase room,
        RoomGroupProvider groups,
        IClock clock,
        ILogger<TimelineService> logger)
    {
        _room = room;
        _groups = groups;
        _clock = clock;
        _logger = logger;
    }

    // クライアントはこの値と自分の時刻の差をクロック差として持ち、再生開始時刻の解釈に使う。
    public UnaryResult<long> GetServerTimeAsync() => new(_clock.UtcNowUnixMs);

    public UnaryResult<TransportState> GetTransportAsync(string roomId)
        => new(ToDto(_room.Get(ParseRoomId(roomId))));

    public UnaryResult<TransportState> PlayAsync(string roomId)
    {
        var id = ParseRoomId(roomId);
        var dto = CommitAndBroadcast(id, () => _room.Play(id));
        _logger.LogInfo("started playback. ", ("RoomId", id.Value), ("StartedAtMs", dto.StartedAtServerMs));
        return new(dto);
    }

    public UnaryResult<TransportState> StopAsync(string roomId)
    {
        var id = ParseRoomId(roomId);
        var dto = CommitAndBroadcast(id, () => _room.Stop(id));
        _logger.LogInfo("stopped playback. ", ("RoomId", id.Value));
        return new(dto);
    }

    public UnaryResult<TransportState> ScheduleActionAsync(string roomId, TimelineAction action)
    {
        var id = ParseRoomId(roomId);

        ScheduledAction scheduled;
        try
        {
            scheduled = ScheduledActionMapper.ToDomain(action);
        }
        catch (DomainException ex)
        {
            _logger.LogWarn("rejected schedule: invalid action. ", ("RoomId", id.Value), ("Reason", ex.Message));
            throw new ReturnStatusException(StatusCode.InvalidArgument, ex.Message);
        }

        var dto = CommitAndBroadcast(id, () => _room.Schedule(id, scheduled));
        _logger.LogInfo("scheduled action. ", ("RoomId", id.Value), ("Offset", scheduled.Offset.ToRawString()), ("Action", scheduled.Action));
        return new(dto);
    }

    public UnaryResult<TransportState> CancelScheduledActionAsync(string roomId)
    {
        var id = ParseRoomId(roomId);
        var dto = CommitAndBroadcast(id, () => _room.CancelSchedule(id));
        _logger.LogInfo("cancelled scheduled action. ", ("RoomId", id.Value));
        return new(dto);
    }

    private RoomId ParseRoomId(string roomId)
    {
        try
        {
            return RoomId.Create(roomId);
        }
        catch (DomainException ex)
        {
            _logger.LogWarn("rejected request: invalid room. ", ("AttemptedRoomId", roomId), ("Reason", ex.Message));
            throw new ReturnStatusException(StatusCode.InvalidArgument, ex.Message);
        }
    }

    // 確定したトランスポートを同じ room の Hub 参加者へ通知し、呼び出し元にも同じ値を返す。
    // Unary は呼び出し元にしか応答できないため、他のクライアントへはこの経路で届ける。
    // 確定と通知を room 単位で直列化し、確定順と配信順がずれないようにする。
    private TransportState CommitAndBroadcast(RoomId roomId, Func<Room> commit)
        => _groups.PublishTransport(roomId, () => ToDto(commit()));

    private TransportState ToDto(Room room) => TransportMapper.ToDto(room, _clock.UtcNowUnixMs);
}
