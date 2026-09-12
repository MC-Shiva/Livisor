using Cysharp.Runtime.Multicast;
using Grpc.Core;
using Livisor.Server.Application.UseCases;
using Livisor.Server.Domain.Time;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Infrastructure;
using Livisor.Server.Presentation.Mapping;
using Livisor.Server.Presentation.Providers;
using Livisor.Server.Presentation.UnaryServices;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;
using MagicOnion;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Livisor.Server.Tests.Presentation;

public class TimelineServiceTests
{
    [Fact]
    public async Task ScheduleBatch_ValidatesAllBeforeAppending_AndCancelKeepsDefaults()
    {
        var defaults = DefaultActionSet.Create().Select(ScheduledActionMapper.ToDomain).ToArray();
        var cache = new RoomCache(defaults);
        var clock = Substitute.For<IClock>();
        clock.UtcNowUnixMs.Returns(1_000);
        var service = new TimelineService(new RoomUseCase(cache, clock),
            new RoomGroupProvider(Substitute.For<IMulticastGroupProvider>()), clock,
            NullLogger<TimelineService>.Instance);
        var valid = new TimelineAction { Time = "00:00:05:00", Action = ActionType.Effect, Value = EffectNames.Lightning };
        var invalid = new TimelineAction { Time = "bad", Action = ActionType.Effect, Value = EffectNames.Lightning };

        foreach (var actions in new[] { new[] { valid, invalid }, new[] { valid, null! }, Array.Empty<TimelineAction>(), null! })
        {
            var error = Assert.Throws<ReturnStatusException>(() => service.ScheduleActionsAsync("room1", actions));
            Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
            Assert.Empty(cache.Get(RoomId.Create("room1")).ScheduledActions);
        }

        var scheduled = await service.ScheduleActionsAsync("room1", new[] { valid, valid });
        Assert.Equal(defaults.Length + 2, scheduled.Actions.Length);
        Assert.Equal(defaults.Length + 3, (await service.ScheduleActionsAsync("room1", new[] { valid })).Actions.Length);
        Assert.Equal(defaults.Length, (await service.GetTransportAsync("room2")).Actions.Length);
        var cancelled = await service.CancelScheduledActionsAsync("room1");
        Assert.Equal(defaults.Length, cancelled.Actions.Length);
        Assert.False(cancelled.Playing);
    }
}
