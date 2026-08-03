using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.DTO;
using NSubstitute;

namespace Livisor.Server.Tests.Application;

public class BroadcastTimelineUseCaseTests
{
    [Fact]
    public void Execute_CallsCacheAddOnce()
    {
        var cache = Substitute.For<ITimelineCache>();
        var useCase = new BroadcastTimelineUseCase(cache);
        var timeline = Timeline.Create([new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.Start, 1)]);

        useCase.Broadcast("room1", timeline);

        cache.Received(1).Add("room1", timeline);
    }
}
