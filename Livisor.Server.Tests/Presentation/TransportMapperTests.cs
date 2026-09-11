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
    public void ToDto_AnyRoom_CarriesDefaultActions()
    {
        // デフォルト演出は room の状態ではなく固定の定義なので、どの room でも同じ内容が載る。
        var dto = TransportMapper.ToDto(NewRoom(), 1_000);

        var expected = DefaultActionSet.Create();
        Assert.Equal(
            expected.Select(a => (a.Time, a.Action, a.Value)),
            dto.DefaultActions.Select(a => (a.Time, a.Action, a.Value)));
    }

    [Fact]
    public void ToDto_CalledTwice_DoesNotShareDefaultActions()
    {
        // DTO は可変。配信ごとに別インスタンスを作り、受信側や別 room の書き換えが混ざらないようにする。
        var first = TransportMapper.ToDto(NewRoom(), 1_000);
        var second = TransportMapper.ToDto(NewRoom(), 1_000);

        Assert.NotSame(first.DefaultActions, second.DefaultActions);
        foreach (var pair in first.DefaultActions.Zip(second.DefaultActions))
            Assert.NotSame(pair.First, pair.Second);
    }

    [Fact]
    public void ToDto_ScheduleAndCancel_KeepDefaultsSeparateFromReservation()
    {
        var room = NewRoom().Play(1_000);
        var defaults = TransportMapper.ToDto(room, 1_000).DefaultActions;
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
            Assert.Equal(
                defaults.Select(a => (a.Time, a.Action, a.Value)),
                state.DefaultActions.Select(a => (a.Time, a.Action, a.Value)));
    }
}
