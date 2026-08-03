using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.DTO;
using NSubstitute;

namespace Livisor.Server.Tests.Application;

public class JoinRoomUseCaseTests
{
    [Fact]
    public void Join_RoomExists_ReturnsAllTimelines()
    {
        var cache = Substitute.For<ITimelineCache>();
        var t1 = Timeline.Create([new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.Start, 1)]);
        var t2 = Timeline.Create([new TimelineItem(PlaybackTime.Parse("11:00:00:00"), ActionType.Stop, 1)]);
        cache.GetAll("room1").Returns([t1, t2]);
        var useCase = new JoinRoomUseCase(cache);

        var result = useCase.Join("room1");

        Assert.Equal(2, result.Count);
        Assert.Same(t1, result[0]);
        Assert.Same(t2, result[1]);
    }

    [Fact]
    public void Join_RoomNotExists_ReturnsEmpty()
    {
        var cache = Substitute.For<ITimelineCache>();
        cache.GetAll("room-none").Returns([]);
        var useCase = new JoinRoomUseCase(cache);

        var result = useCase.Join("room-none");

        Assert.Empty(result);
    }
}