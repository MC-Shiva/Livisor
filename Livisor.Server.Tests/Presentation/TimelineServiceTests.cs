using Cysharp.Runtime.Multicast;
using Cysharp.Runtime.Multicast.InMemory;
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
using Livisor.Shared.Hubs;
using MessagePack;
using MagicOnion;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Livisor.Server.Tests.Presentation;

public class TimelineServiceTests
{
    [Fact]
    public async Task FireEffect_ValidCommands_DeliversEachTimeOnlyToCurrentRoomMembers()
    {
        var clock = Substitute.For<IClock>();
        var groups = new RoomGroupProvider(new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance));
        var cache = new RoomCache(DefaultTimeline.Create().Select(ScheduledActionMapper.ToDomain).ToArray());
        var service = new TimelineService(new RoomUseCase(cache, clock), groups, clock, NullLogger<TimelineService>.Instance);
        var room = RoomId.Create("room1");
        var first = Substitute.For<IRoomStateHubReceiver>();
        var second = Substitute.For<IRoomStateHubReceiver>();
        var other = Substitute.For<IRoomStateHubReceiver>();
        var firstId = Guid.NewGuid();
        groups.Join(room, firstId, first, () => new RoomStatePatch());
        groups.Join(room, Guid.NewGuid(), second, () => new RoomStatePatch());
        groups.Join(RoomId.Create("room2"), Guid.NewGuid(), other, () => new RoomStatePatch());
        await service.PlayAsync("room1");
        var before = await service.GetTransportAsync("room1");

        var effects = new[]
        {
            new EffectCommand { Name = EffectNames.Lightning, Target = LightningTargets.UnityChan },
            new EffectCommand { Name = EffectNames.Lightning, Target = LightningTargets.Audience },
            new EffectCommand { Name = EffectNames.Lightning, Target = LightningTargets.Stage },
            new EffectCommand { Name = EffectNames.SilverStreamer },
            new EffectCommand { Name = EffectNames.ConfettiOn },
            new EffectCommand { Name = EffectNames.ConfettiOff },
        };
        foreach (var effect in effects)
        {
            var restored = MessagePackSerializer.Deserialize<EffectCommand>(MessagePackSerializer.Serialize(effect));
            Assert.Equal(effect.Name, restored.Name);
            Assert.Equal(effect.Target, restored.Target);
            await service.FireEffectAsync("room1", restored);
            await service.FireEffectAsync("room1", restored);
            first.Received(2).OnEffectTriggered(Arg.Is<EffectCommand>(e => e != null && e.Name == effect.Name && e.Target == effect.Target));
            second.Received(2).OnEffectTriggered(Arg.Is<EffectCommand>(e => e != null && e.Name == effect.Name && e.Target == effect.Target));
        }
        other.DidNotReceive().OnEffectTriggered(Arg.Any<EffectCommand>());
        var after = await service.GetTransportAsync("room1");
        Assert.Equal(MessagePackSerializer.Serialize(before), MessagePackSerializer.Serialize(after));

        groups.Leave(room, firstId);
        first.ClearReceivedCalls();
        await service.FireEffectAsync("room1", effects[0]);
        first.DidNotReceive().OnEffectTriggered(Arg.Any<EffectCommand>());
        groups.Join(room, firstId, first, () => new RoomStatePatch());
        first.DidNotReceive().OnEffectTriggered(Arg.Any<EffectCommand>());
        await service.StopAsync("room1");
        var error = Assert.Throws<ReturnStatusException>(() => service.FireEffectAsync("room1", effects[0]));
        Assert.Equal(StatusCode.FailedPrecondition, error.StatusCode);
        first.DidNotReceive().OnEffectTriggered(Arg.Any<EffectCommand>());
    }

    [Fact]
    public async Task FireEffect_InvalidRequest_RejectsWithoutBroadcast()
    {
        var clock = Substitute.For<IClock>();
        var groups = new RoomGroupProvider(new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance));
        var service = new TimelineService(new RoomUseCase(new RoomCache(), clock), groups, clock, NullLogger<TimelineService>.Instance);
        var receiver = Substitute.For<IRoomStateHubReceiver>();
        groups.Join(RoomId.Create("room1"), Guid.NewGuid(), receiver, () => new RoomStatePatch());
        var valid = new EffectCommand { Name = EffectNames.SilverStreamer };
        Assert.Equal(StatusCode.FailedPrecondition,
            Assert.Throws<ReturnStatusException>(() => service.FireEffectAsync("room1", valid)).StatusCode);
        await service.PlayAsync("room1");
        foreach (var effect in new EffectCommand[]
        {
            null!, new(), new() { Name = "unknown" },
            new() { Name = EffectNames.Lightning },
            new() { Name = EffectNames.Lightning, Target = "unknown" },
            new() { Name = EffectNames.ConfettiOn, Target = LightningTargets.Stage },
        })
            Assert.Equal(StatusCode.InvalidArgument,
                Assert.Throws<ReturnStatusException>(() => service.FireEffectAsync("room1", effect)).StatusCode);
        Assert.Equal(StatusCode.InvalidArgument,
            Assert.Throws<ReturnStatusException>(() => service.FireEffectAsync(" ", valid)).StatusCode);
        receiver.DidNotReceive().OnEffectTriggered(Arg.Any<EffectCommand>());
        Assert.Equal(1, groups.ActiveGroupCount);
    }

    [Fact]
    public async Task ScheduleBatch_ValidatesAllBeforeAppending_AndCancelKeepsDefaults()
    {
        var defaults = DefaultTimeline.Create().Select(ScheduledActionMapper.ToDomain).ToArray();
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
