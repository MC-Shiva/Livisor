using Livisor.Shared.Common;
using Livisor.Shared.DTO;
using MessagePack;

namespace Livisor.Server.Tests.Presentation;

public class TransportStateSerializationTests
{
    [Fact]
    public void Roundtrip_PreservesQueueAndTransport()
    {
        var state = new TransportState
        {
            Playing = true, StartedAtServerMs = 1_000, ServerTimeMs = 2_000,
            Actions = DefaultActionSet.Create().Concat(new[]
            {
                new TimelineAction { Time = "00:00:10:00", Action = ActionType.VolumeChange, Value = 42 },
                new TimelineAction { Time = "00:00:11:00", Action = ActionType.Play, Value = false },
            }).ToArray(),
        };
        var restored = MessagePackSerializer.Deserialize<TransportState>(MessagePackSerializer.Serialize(state));

        Assert.Equal(state.Playing, restored.Playing);
        Assert.Equal(state.StartedAtServerMs, restored.StartedAtServerMs);
        Assert.Equal(state.ServerTimeMs, restored.ServerTimeMs);
        Assert.Equal(state.Actions.Select(a => (a.Time, a.Action, a.Value)),
            restored.Actions.Select(a => (a.Time, a.Action, a.Value)));
    }

    [Fact]
    public void Roundtrip_EmptyQueue()
    {
        var restored = MessagePackSerializer.Deserialize<TransportState>(MessagePackSerializer.Serialize(new TransportState()));
        Assert.Empty(restored.Actions);
    }
}
