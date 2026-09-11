using System.Buffers;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;
using MessagePack;

namespace Livisor.Server.Tests.Presentation;

// 操作の通信形式と、事前定義を含んでいた過去の形式の読み取りを検証する。
public class TransportStateSerializationTests
{
    [Theory]
    [InlineData(EffectNames.ConfettiOn)]
    [InlineData(EffectNames.ConfettiOff)]
    [InlineData(EffectNames.Lightning)]
    [InlineData(EffectNames.SilverStreamer)]
    public void Roundtrip_PreservesScheduledEffect(string effectName)
    {
        var state = new TransportState
        {
            Playing = true,
            StartedAtServerMs = 1_000,
            ServerTimeMs = 2_000,
            ScheduledAction = new TimelineAction
            {
                Time = "00:01:30:00", Action = ActionType.Effect, Value = effectName,
            },
        };

        var restored = MessagePackSerializer.Deserialize<TransportState>(MessagePackSerializer.Serialize(state));

        Assert.Equal(state.Playing, restored.Playing);
        Assert.Equal(state.StartedAtServerMs, restored.StartedAtServerMs);
        Assert.Equal(state.ServerTimeMs, restored.ServerTimeMs);
        Assert.NotNull(restored.ScheduledAction);
        Assert.Equal(state.ScheduledAction.Time, restored.ScheduledAction.Time);
        Assert.Equal(state.ScheduledAction.Action, restored.ScheduledAction.Action);
        Assert.Equal(state.ScheduledAction.Value, restored.ScheduledAction.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Deserialize_PreviousFormat_IgnoresPresetActions(bool includesPresetActions)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(includesPresetActions ? 5 : 4);
        writer.Write(true);
        writer.Write(1_000L);
        writer.Write(2_000L);
        writer.WriteNil();
        if (includesPresetActions)
            writer.WriteRaw(MessagePackSerializer.Serialize(DefaultActionSet.Create()));
        writer.Flush();

        var restored = MessagePackSerializer.Deserialize<TransportState>(buffer.WrittenMemory);

        Assert.True(restored.Playing);
        Assert.Null(restored.ScheduledAction);
        Assert.Equal(1_000, restored.StartedAtServerMs);
        Assert.Equal(2_000, restored.ServerTimeMs);
    }
}
