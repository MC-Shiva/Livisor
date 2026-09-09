using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;

namespace Livisor.Server.Tests.Domain;

public class ScheduledActionTests
{
    [Fact]
    public void FireAtUnixMs_AddsOffsetToStartTime()
    {
        var action = new ScheduledAction(PlaybackTime.Parse("00:00:03:00"), ActionType.Play, true);

        Assert.Equal(1_000 + 3_000, action.FireAtUnixMs(1_000));
    }

    [Fact]
    public void FireAtUnixMs_CentisecondOffset_KeepsExactMilliseconds()
    {
        // センチ秒は 10 ミリ秒単位。秒の小数を経由せず整数のまま換算する。
        var action = new ScheduledAction(PlaybackTime.Parse("00:00:00:07"), ActionType.VolumeChange, 10);

        Assert.Equal(70, action.FireAtUnixMs(0));
    }

    [Fact]
    public void FireAtUnixMs_ZeroOffset_ReturnsStartTime()
    {
        var action = new ScheduledAction(PlaybackTime.Parse("00:00:00:00"), ActionType.Play, true);

        Assert.Equal(1_700_000_000_000, action.FireAtUnixMs(1_700_000_000_000));
    }

    [Fact]
    public void FireAtUnixMs_LargeOffset_DoesNotOverflow()
    {
        // 23:59:59:99 は 86,399,990 ミリ秒。Unix ミリ秒に足しても long の範囲に収まる。
        var action = new ScheduledAction(PlaybackTime.Parse("23:59:59:99"), ActionType.Play, false);

        Assert.Equal(1_700_000_000_000 + 86_399_990, action.FireAtUnixMs(1_700_000_000_000));
    }

    [Fact]
    public void Constructor_PlayWithNumber_ThrowsDomainException()
    {
        // play は true/false に統一されている（Issue #11）。数値は受け付けない。
        Assert.Throws<DomainException>(
            () => new ScheduledAction(PlaybackTime.Parse("00:00:05:00"), ActionType.Play, 1));
    }

    [Fact]
    public void Constructor_VolumeChangeWithBool_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(
            () => new ScheduledAction(PlaybackTime.Parse("00:00:05:00"), ActionType.VolumeChange, true));
    }

    [Fact]
    public void Constructor_UnknownActionType_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(
            () => new ScheduledAction(PlaybackTime.Parse("00:00:05:00"), (ActionType)999, true));
    }
}
