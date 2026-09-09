using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Tests.Domain;

public class TransportTests
{
    [Fact]
    public void Stopped_HasNoStartTime()
    {
        Assert.False(Transport.Stopped.Playing);
        Assert.Null(Transport.Stopped.StartedAtUnixMs);
    }

    [Fact]
    public void Start_FromStopped_RecordsStartTime()
    {
        var transport = Transport.Stopped.Start(1_700_000_000_000);

        Assert.True(transport.Playing);
        Assert.Equal(1_700_000_000_000, transport.StartedAtUnixMs);
    }

    [Fact]
    public void Start_WhilePlaying_KeepsFirstStartTime()
    {
        // 開始時刻を動かすと予約アクションの発火位置がずれるため、再生中の再開始は無視する。
        var transport = Transport.Stopped.Start(1_000).Start(9_999);

        Assert.Equal(1_000, transport.StartedAtUnixMs);
    }

    [Fact]
    public void Start_NegativeTime_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Transport.Stopped.Start(-1));
    }

    [Fact]
    public void Start_DoesNotMutateOriginalInstance()
    {
        var stopped = Transport.Stopped;

        stopped.Start(1_000);

        Assert.False(stopped.Playing);
    }

    [Fact]
    public void Stop_WhilePlaying_ClearsStartTime()
    {
        var transport = Transport.Stopped.Start(1_000).Stop();

        Assert.False(transport.Playing);
        Assert.Null(transport.StartedAtUnixMs);
    }

    [Fact]
    public void Stop_WhileStopped_ReturnsSameInstance()
    {
        Assert.Same(Transport.Stopped, Transport.Stopped.Stop());
    }

    [Fact]
    public void PositionMs_WhilePlaying_ReturnsElapsedMs()
    {
        var transport = Transport.Stopped.Start(1_000);

        Assert.Equal(2_500, transport.PositionMs(3_500));
    }

    [Fact]
    public void PositionMs_WhileStopped_ReturnsZero()
    {
        Assert.Equal(0, Transport.Stopped.PositionMs(3_500));
    }

    [Fact]
    public void PositionMs_BeforeStartTime_ReturnsZero()
    {
        // クロックの巻き戻りなどで開始前の時刻を渡されても、負の再生位置は返さない。
        var transport = Transport.Stopped.Start(5_000);

        Assert.Equal(0, transport.PositionMs(4_000));
    }
}
