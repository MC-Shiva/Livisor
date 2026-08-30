using Livisor.Server.Domain.Entity;
using Livisor.Shared.DTO;

namespace Livisor.Server.Presentation.Mapping;

// Domain → Shared.DTO の変換（トランスポート）。
// 停止中は再生開始時刻を持たないため、ワイヤ上は 0 で表す（Playing で判別できる）。
// DTO の TransportState は Domain の Transport（再生状態）と Room.Scheduled（予約）を 1 つに
// まとめた合成ビューである。Domain の Transport と 1:1 対応しないため Room を受け取る。
public static class TransportMapper
{
    public static TransportState ToDto(Room room, long serverTimeMs) => new()
    {
        Playing = room.Transport.Playing,
        StartedAtServerMs = room.Transport.StartedAtUnixMs ?? 0,
        ServerTimeMs = serverTimeMs,
        ScheduledAction = room.Scheduled is null ? null : ScheduledActionMapper.ToDto(room.Scheduled),
    };
}
