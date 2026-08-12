using Livisor.Shared.Common;
using Livisor.Shared.DTO;
using MessagePack;

namespace Livisor.Server.Tests.Presentation;

// ActionValue の MessagePack ラウンドトリップ検証。
// Docs/Rules/test.md は Domain / Application / Infrastructure の 3 層を規定するが、
// ワイヤ形式（Presentation 境界）の検証はどの既存層にも当てはまらないため、ここに追加する。
public class ActionValueTests
{
    [Fact]
    public void Roundtrip_Number_PreservesKindAndValue()
    {
        var value = ActionValue.From(1);

        var bytes = MessagePackSerializer.Serialize(value);
        var restored = MessagePackSerializer.Deserialize<ActionValue>(bytes);

        Assert.Equal(ActionValueKind.Number, restored.Kind);
        Assert.Equal(1, restored.Number);
    }

    [Fact]
    public void Roundtrip_Bool_PreservesKindAndValue()
    {
        var value = ActionValue.From(true);

        var bytes = MessagePackSerializer.Serialize(value);
        var restored = MessagePackSerializer.Deserialize<ActionValue>(bytes);

        Assert.Equal(ActionValueKind.Bool, restored.Kind);
        Assert.True(restored.Bool);
    }

    [Fact]
    public void Roundtrip_Text_PreservesKindAndValue()
    {
        var value = ActionValue.From("intro");

        var bytes = MessagePackSerializer.Serialize(value);
        var restored = MessagePackSerializer.Deserialize<ActionValue>(bytes);

        Assert.Equal(ActionValueKind.Text, restored.Kind);
        Assert.Equal("intro", restored.Text);
    }

    [Fact]
    public void Roundtrip_TimelineAction_PreservesValue()
    {
        var action = new TimelineAction { Time = "10:00:00:00", Action = ActionType.Start, Value = true };

        var bytes = MessagePackSerializer.Serialize(action);
        var restored = MessagePackSerializer.Deserialize<TimelineAction>(bytes);

        Assert.Equal(ActionValueKind.Bool, restored.Value.Kind);
        Assert.True(restored.Value.Bool);
    }

    [Fact]
    public void Serialize_Number_UsesRawMessagePackInteger()
    {
        // ワイヤ上は ActionValue のタグではなく生の MessagePack 整数 1 バイト（fixint）になること。
        var value = ActionValue.From(1);

        var bytes = MessagePackSerializer.Serialize(value);

        Assert.Single(bytes);
        Assert.Equal(1, MessagePackSerializer.Deserialize<int>(bytes));
    }
}
