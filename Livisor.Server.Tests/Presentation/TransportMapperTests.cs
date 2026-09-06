using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Presentation.Mapping;
using Livisor.Shared.Common;

namespace Livisor.Server.Tests.Presentation;

// Domain → DTO 変換（トランスポート）の検証。
public class TransportMapperTests
{
    private static Room NewRoom() => Room.Create(RoomId.Create("room1"));

    [Fact]
    public void ToDto_StoppedRoom_ReportsZeroStartTime()
    {
        // 停止中は開始時刻を持たない。ワイヤ上は 0 で表し、Playing で判別する。
        var dto = TransportMapper.ToDto(NewRoom(), 1_700_000_000_000);

        Assert.False(dto.Playing);
        Assert.Equal(0, dto.StartedAtServerMs);
        Assert.Equal(1_700_000_000_000, dto.ServerTimeMs);
        Assert.Null(dto.ScheduledAction);
    }

    [Fact]
    public void ToDto_PlayingRoom_CarriesStartTime()
    {
        var dto = TransportMapper.ToDto(NewRoom().Play(1_700_000_000_000), 1_700_000_005_000);

        Assert.True(dto.Playing);
        Assert.Equal(1_700_000_000_000, dto.StartedAtServerMs);
        Assert.Equal(1_700_000_005_000, dto.ServerTimeMs);
    }

    [Fact]
    public void ToDto_ScheduledRoom_CarriesActionAsRelativeTime()
    {
        var room = NewRoom().Schedule(new ScheduledAction(PlaybackTime.Parse("00:00:05:00"), ActionType.VolumeChange, 10));

        var dto = TransportMapper.ToDto(room, 1_000);

        Assert.NotNull(dto.ScheduledAction);
        Assert.Equal("00:00:05:00", dto.ScheduledAction!.Time);
        Assert.Equal(ActionType.VolumeChange, dto.ScheduledAction.Action);
        Assert.Equal(10, dto.ScheduledAction.Value.Number);
    }
}
