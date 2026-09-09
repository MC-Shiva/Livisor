using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Presentation.Mapping;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;

namespace Livisor.Server.Tests.Presentation;

// DTO ↔ Domain 変換（同期状態）の検証。
public class RoomStateMapperTests
{
    [Fact]
    public void ToDomain_Entries_ReturnsRoomState()
    {
        var entries = new[]
        {
            new RoomStateEntry { Key = RoomStateKeys.HeartRate, Value = 82 },
            new RoomStateEntry { Key = "lightColor", Value = "red" },
        };

        var state = RoomStateMapper.ToDomain(entries);

        Assert.Equal(82, state.Entries[RoomStateKeys.HeartRate].Number);
        Assert.Equal("red", state.Entries["lightColor"].Text);
    }

    [Fact]
    public void ToDomain_EmptyArray_ReturnsEmptyState()
    {
        Assert.Empty(RoomStateMapper.ToDomain([]).Entries);
    }

    [Fact]
    public void ToDomain_Null_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => RoomStateMapper.ToDomain(null!));
    }

    [Fact]
    public void ToDomain_NullEntry_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => RoomStateMapper.ToDomain([null!]));
    }

    [Fact]
    public void ToDomain_KnownKeyWithWrongValueKind_ThrowsDomainException()
    {
        var entries = new[] { new RoomStateEntry { Key = RoomStateKeys.Volume, Value = "loud" } };

        Assert.Throws<DomainException>(() => RoomStateMapper.ToDomain(entries));
    }

    [Fact]
    public void ToDto_CarriesEntriesAndServerTime()
    {
        var state = RoomState.Create([new KeyValuePair<string, ActionValue>(RoomStateKeys.Volume, 30)]);

        var dto = RoomStateMapper.ToDto(state, 1_700_000_000_000);

        Assert.Equal(1_700_000_000_000, dto.ServerTimeMs);
        var entry = Assert.Single(dto.Entries);
        Assert.Equal(RoomStateKeys.Volume, entry.Key);
        Assert.Equal(30, entry.Value.Number);
    }
}
