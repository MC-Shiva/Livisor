using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;

namespace Livisor.Server.Tests.Domain;

public class RoomStateTests
{
    private static RoomState Build(params (string Key, ActionValue Value)[] entries)
        => RoomState.Create(entries.Select(e => new KeyValuePair<string, ActionValue>(e.Key, e.Value)).ToList());

    [Fact]
    public void Empty_HasNoEntries()
    {
        Assert.Empty(RoomState.Empty.Entries);
    }

    [Fact]
    public void Create_Null_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => RoomState.Create(null!));
    }

    [Fact]
    public void Create_WithEntries_KeepsAll()
    {
        var state = Build((RoomStateKeys.HeartRate, 82), (RoomStateKeys.Volume, 30));

        Assert.Equal(2, state.Entries.Count);
        Assert.Equal(82, state.Entries[RoomStateKeys.HeartRate].Number);
        Assert.Equal(30, state.Entries[RoomStateKeys.Volume].Number);
    }

    [Fact]
    public void Create_UnknownKey_IsAccepted()
    {
        // 同期する項目は公演ごとに増えるため、未知のキーは可変項目として受け入れる。
        var state = Build(("lightColor", "red"));

        Assert.Equal("red", state.Entries["lightColor"].Text);
    }

    [Fact]
    public void Create_EmptyKey_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Build((" ", 1)));
    }

    [Fact]
    public void Create_KnownKeyWithWrongValueKind_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Build((RoomStateKeys.HeartRate, true)));
    }

    [Fact]
    public void Create_DuplicateKey_KeepsLastValue()
    {
        var state = Build((RoomStateKeys.HeartRate, 70), (RoomStateKeys.HeartRate, 90));

        Assert.Equal(90, state.Entries[RoomStateKeys.HeartRate].Number);
    }

    [Fact]
    public void Merge_AddsNewKeysAndOverwritesExisting()
    {
        var current = Build((RoomStateKeys.HeartRate, 70), (RoomStateKeys.Volume, 30));
        var patch = Build((RoomStateKeys.HeartRate, 95), ("lightColor", "blue"));

        var merged = current.Merge(patch);

        Assert.Equal(95, merged.Entries[RoomStateKeys.HeartRate].Number);
        Assert.Equal(30, merged.Entries[RoomStateKeys.Volume].Number);
        Assert.Equal("blue", merged.Entries["lightColor"].Text);
    }

    [Fact]
    public void Merge_DoesNotMutateOriginalInstance()
    {
        // RoomState は不変。Merge は新しいインスタンスを返し、元のインスタンスは変わらない。
        var current = Build((RoomStateKeys.HeartRate, 70));

        current.Merge(Build((RoomStateKeys.HeartRate, 95)));

        Assert.Equal(70, current.Entries[RoomStateKeys.HeartRate].Number);
    }

    [Fact]
    public void Merge_EmptyPatch_ReturnsSameInstance()
    {
        var current = Build((RoomStateKeys.HeartRate, 70));

        Assert.Same(current, current.Merge(RoomState.Empty));
    }

    [Fact]
    public void Merge_Null_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => RoomState.Empty.Merge(null!));
    }

    [Fact]
    public void Entries_CastToMutableDictionary_CannotBeModified()
    {
        // 内部の Dictionary をそのまま返していると、キャストしてロックを迂回した変更ができてしまう。
        var state = RoomState.Create([new KeyValuePair<string, ActionValue>(RoomStateKeys.Volume, 30)]);

        Assert.Throws<NotSupportedException>(
            () => ((IDictionary<string, ActionValue>)state.Entries).Add("injected", 1));
    }
}
