using System.Buffers;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;
using MessagePack;

namespace Livisor.Server.Tests.Presentation;

// ActionValue の MessagePack ラウンドトリップ検証。
// Docs/Rules/test.md は Domain / Application / Infrastructure の 3 層を規定するが、
// ワイヤ形式（Presentation 境界）の検証はどの既存層にも当てはまらないため、ここに追加する。
// ActionValue は単体では直列化できない（MessagePackFormatter 属性は TimelineAction.Value に
// 付いている）ため、ラウンドトリップは TimelineAction 経由で検証する。
public class ActionValueTests
{
    [Fact]
    public void Roundtrip_Number_PreservesKindAndValue()
    {
        var action = new TimelineAction { Time = "10:00:00:00", Action = ActionType.VolumeChange, Value = 1 };

        var bytes = MessagePackSerializer.Serialize(action);
        var restored = MessagePackSerializer.Deserialize<TimelineAction>(bytes);

        Assert.Equal(ActionValueKind.Number, restored.Value.Kind);
        Assert.Equal(1, restored.Value.Number);
    }

    [Fact]
    public void Roundtrip_Bool_PreservesKindAndValue()
    {
        var action = new TimelineAction { Time = "10:00:00:00", Action = ActionType.Play, Value = true };

        var bytes = MessagePackSerializer.Serialize(action);
        var restored = MessagePackSerializer.Deserialize<TimelineAction>(bytes);

        Assert.Equal(ActionValueKind.Bool, restored.Value.Kind);
        Assert.True(restored.Value.Bool);
    }

    [Fact]
    public void Roundtrip_BoolFalse_PreservesKindAndValue()
    {
        // play=false（停止）は bool の既定値と紛れやすいため、Kind と値の両方を検証する。
        var action = new TimelineAction { Time = "10:00:00:00", Action = ActionType.Play, Value = false };

        var bytes = MessagePackSerializer.Serialize(action);
        var restored = MessagePackSerializer.Deserialize<TimelineAction>(bytes);

        Assert.Equal(ActionValueKind.Bool, restored.Value.Kind);
        Assert.False(restored.Value.Bool);
    }

    [Fact]
    public void Roundtrip_Text_PreservesKindAndValue()
    {
        var action = new TimelineAction { Time = "10:00:00:00", Action = ActionType.Play, Value = "intro" };

        var bytes = MessagePackSerializer.Serialize(action);
        var restored = MessagePackSerializer.Deserialize<TimelineAction>(bytes);

        Assert.Equal(ActionValueKind.Text, restored.Value.Kind);
        Assert.Equal("intro", restored.Value.Text);
    }

    [Fact]
    public void Roundtrip_TimelineAction_PreservesValue()
    {
        var action = new TimelineAction { Time = "10:00:00:00", Action = ActionType.Play, Value = true };

        var bytes = MessagePackSerializer.Serialize(action);
        var restored = MessagePackSerializer.Deserialize<TimelineAction>(bytes);

        Assert.Equal(ActionValueKind.Bool, restored.Value.Kind);
        Assert.True(restored.Value.Bool);
    }

    [Fact]
    public void Serialize_Number_UsesRawMessagePackInteger()
    {
        // ワイヤ上は ActionValue のタグではなく生の MessagePack 整数 1 バイト（fixint）になること。
        // ActionValueFormatter を直接呼び、TimelineAction の Time/Action フィールドの影響を排除して検証する。
        var formatter = new ActionValueFormatter();
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        formatter.Serialize(ref writer, ActionValue.From(1), MessagePackSerializerOptions.Standard);
        writer.Flush();

        Assert.Single(buffer.WrittenSpan.ToArray());
        Assert.Equal(1, MessagePackSerializer.Deserialize<int>(buffer.WrittenMemory));
    }
}
