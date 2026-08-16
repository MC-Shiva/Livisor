using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;
using NSubstitute;

namespace Livisor.Server.Tests.Application;

public class BroadcastTimelineUseCaseTests
{
    [Fact]
    public void Execute_CallsCacheSetCurrentTimelineOnce()
    {
        var cache = Substitute.For<IRoomCache>();
        var useCase = new BroadcastTimelineUseCase(cache);
        var roomId = RoomId.Create("room1");
        var timeline = Timeline.Create([new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.Play, true)]);

        useCase.Broadcast(roomId, timeline);

        cache.Received(1).SetCurrentTimeline(roomId, timeline);
    }
}
