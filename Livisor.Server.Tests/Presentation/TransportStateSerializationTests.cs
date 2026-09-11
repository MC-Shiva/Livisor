using System.Buffers;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;
using MessagePack;

namespace Livisor.Server.Tests.Presentation;

// TransportState の MessagePack ラウンドトリップと、DefaultActions（Key 4）を知らない旧形式との互換の検証。
public class TransportStateSerializationTests
{
    [Fact]
    public void Roundtrip_WithDefaultActions_PreservesThem()
    {
        var state = new TransportState
        {
            Playing = true,
            DefaultActions = DefaultActionSet.Create().Concat(new[]
            {
                new TimelineAction { Time = "00:00:30:00", Action = ActionType.VolumeChange, Value = 50 },
                new TimelineAction { Time = "00:03:55:00", Action = ActionType.Play, Value = false },
            }).ToArray(),
            ScheduledAction = new TimelineAction
            {
                Time = "00:01:30:00", Action = ActionType.Effect, Value = EffectNames.SilverStreamer,
            },
        };

        var restored = MessagePackSerializer.Deserialize<TransportState>(MessagePackSerializer.Serialize(state));

        Assert.Equal(
            state.DefaultActions.Select(a => (a.Time, a.Action, a.Value)),
            restored.DefaultActions.Select(a => (a.Time, a.Action, a.Value)));
        Assert.NotNull(restored.ScheduledAction);
        Assert.Equal(state.ScheduledAction.Time, restored.ScheduledAction.Time);
        Assert.Equal(state.ScheduledAction.Action, restored.ScheduledAction.Action);
        Assert.Equal(state.ScheduledAction.Value, restored.ScheduledAction.Value);
    }

    [Fact]
    public void Deserialize_OldFormatWithoutDefaultActions_KeepsEmptyArray()
    {
        // Key(4) を持たない旧サーバーの配信は 4 要素の配列。読まれなかった項目は初期値（空配列）のまま残る。
        // 受信側はそれでも念のため null を空として扱う（TransportState のコメント参照）。
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(4);
        writer.Write(true);
        writer.Write(1_000L);
        writer.Write(2_000L);
        writer.WriteNil();
        writer.Flush();

        var restored = MessagePackSerializer.Deserialize<TransportState>(buffer.WrittenMemory);

        Assert.True(restored.Playing);
        Assert.Null(restored.ScheduledAction);
        Assert.NotNull(restored.DefaultActions);
        Assert.Empty(restored.DefaultActions);
    }
}
