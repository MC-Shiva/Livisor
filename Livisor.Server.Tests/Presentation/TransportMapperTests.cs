using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Presentation.Mapping;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;

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
        Assert.Empty(dto.Actions);
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

        Assert.Single(dto.Actions);
        Assert.Equal("00:00:05:00", dto.Actions[0].Time);
        Assert.Equal(ActionType.VolumeChange, dto.Actions[0].Action);
        Assert.Equal(10, dto.Actions[0].Value.Number);
    }

    [Fact]
    public void ToDto_MergesDefaultsAndAdditionsInTimeOrder_AndCancelKeepsDefaults()
    {
        var defaults = DefaultTimeline.Create().Select(ScheduledActionMapper.ToDomain).ToArray();
        var room = Room.Create(RoomId.Create("room1"), defaults)
            .Schedule(new ScheduledAction(PlaybackTime.Parse("00:01:00:00"), ActionType.Effect, EffectNames.ConfettiOff),
                new ScheduledAction(PlaybackTime.Parse("00:00:05:00"), ActionType.Effect, EffectNames.SilverStreamer));
        var dto = TransportMapper.ToDto(room, 1_000);

        Assert.Equal(defaults.Length + 2, dto.Actions.Length);
        Assert.Equal(new[] { EffectNames.ConfettiOn, EffectNames.SilverStreamer, EffectNames.Lightning, EffectNames.ConfettiOff },
            dto.Actions.Take(4).Select(a => a.Value.Text));
        Assert.Equal(defaults.Length, TransportMapper.ToDto(room.CancelSchedule(), 2_000).Actions.Length);
    }

    [Fact]
    public void ToDto_CalledTwice_DoesNotShareScheduledAction()
    {
        var room = NewRoom().Schedule(new ScheduledAction(PlaybackTime.Parse("00:00:05:00"), ActionType.Effect, EffectNames.ConfettiOff));
        var first = TransportMapper.ToDto(room, 1_000);
        first.Actions[0].Value = "modified";
        var second = TransportMapper.ToDto(room, 1_000);

        Assert.NotSame(first.Actions[0], second.Actions[0]);
        Assert.Equal(EffectNames.ConfettiOff, second.Actions[0].Value.Text);
    }

    [Fact]
    public void ToDto_ScheduleAndCancel_OnlyChangesReservation()
    {
        var room = NewRoom().Play(1_000);
        var action = new ScheduledAction(PlaybackTime.Parse("00:01:30:00"), ActionType.Effect, EffectNames.SilverStreamer);

        room = room.Schedule(action);
        var scheduled = TransportMapper.ToDto(room, 2_000);
        Assert.Equal("00:01:30:00", scheduled.Actions[0].Time);
        Assert.Equal(EffectNames.SilverStreamer, scheduled.Actions[0].Value.Text);

        // 時刻を過ぎても元の予約を配信する。受信後の即時実行はクライアントの責務。
        var past = TransportMapper.ToDto(room, 100_000);
        Assert.Equal(scheduled.Actions[0].Time, past.Actions[0].Time);
        Assert.True(past.ServerTimeMs - past.StartedAtServerMs > action.Offset.TotalCentiseconds * 10L);

        var cancelled = TransportMapper.ToDto(room.CancelSchedule(), 101_000);
        Assert.Empty(cancelled.Actions);
        foreach (var state in new[] { scheduled, past, cancelled })
        {
            Assert.True(state.Playing);
            Assert.Equal(1_000, state.StartedAtServerMs);
        }
    }
}
