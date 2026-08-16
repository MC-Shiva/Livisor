using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;
using NSubstitute;

namespace Livisor.Server.Tests.Application;

public class JoinRoomUseCaseTests
{
    [Fact]
    public void Join_RoomExists_ReturnsCurrentTimeline()
    {
        var cache = Substitute.For<IRoomCache>();
        var roomId = RoomId.Create("room1");
        var t1 = Timeline.Create([new TimelineItem(PlaybackTime.Parse("10:00:00:00"), ActionType.Play, true)]);
        var room = Room.Create(roomId).SetCurrent(t1);
        cache.Get(roomId).Returns(room);
        var useCase = new JoinRoomUseCase(cache);

        var result = useCase.Join(roomId);

        Assert.Same(t1, result);
    }

    [Fact]
    public void Join_RoomNotExists_ReturnsNull()
    {
        var cache = Substitute.For<IRoomCache>();
        var roomId = RoomId.Create("room-none");
        cache.Get(roomId).Returns(Room.Create(roomId));
        var useCase = new JoinRoomUseCase(cache);

        var result = useCase.Join(roomId);

        Assert.Null(result);
    }
}
