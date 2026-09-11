using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Presentation.Mapping;
using Livisor.Shared.Common;
using MessagePack;

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

    [Fact]
    public void ToDto_AnyRoom_DoesNotSendPresetActions()
    {
        var dto = TransportMapper.ToDto(NewRoom(), 1_000);

        var reader = new MessagePackReader(MessagePackSerializer.Serialize(dto));
        Assert.Equal(4, reader.ReadArrayHeader());
    }

    [Fact]
    public void ToDto_CalledTwice_DoesNotShareScheduledAction()
    {
        var room = NewRoom().Schedule(new ScheduledAction(PlaybackTime.Parse("00:00:05:00"), ActionType.Effect, EffectNames.ConfettiOff));
        var first = TransportMapper.ToDto(room, 1_000);
        first.ScheduledAction!.Value = "modified";
        var second = TransportMapper.ToDto(room, 1_000);

        Assert.NotSame(first.ScheduledAction, second.ScheduledAction);
        Assert.Equal(EffectNames.ConfettiOff, second.ScheduledAction!.Value.Text);
    }

    [Fact]
    public void ToDto_ScheduleAndCancel_OnlyChangesReservation()
    {
        var room = NewRoom().Play(1_000);
        var action = new ScheduledAction(PlaybackTime.Parse("00:01:30:00"), ActionType.Effect, EffectNames.SilverStreamer);

        room = room.Schedule(action);
        var scheduled = TransportMapper.ToDto(room, 2_000);
        Assert.Equal("00:01:30:00", scheduled.ScheduledAction!.Time);
        Assert.Equal(EffectNames.SilverStreamer, scheduled.ScheduledAction.Value.Text);

        // 時刻を過ぎても元の予約を配信する。受信後の即時実行はクライアントの責務。
        var past = TransportMapper.ToDto(room, 100_000);
        Assert.Equal(scheduled.ScheduledAction.Time, past.ScheduledAction!.Time);
        Assert.True(past.ServerTimeMs - past.StartedAtServerMs > action.Offset.TotalCentiseconds * 10L);

        var cancelled = TransportMapper.ToDto(room.CancelSchedule(), 101_000);
        Assert.Null(cancelled.ScheduledAction);
        foreach (var state in new[] { scheduled, past, cancelled })
        {
            Assert.True(state.Playing);
            Assert.Equal(1_000, state.StartedAtServerMs);
        }
    }
}
