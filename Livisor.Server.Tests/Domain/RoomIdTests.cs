using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Tests.Domain;

public class RoomIdTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhiteSpace_ThrowsDomainException(string? value)
    {
        Assert.Throws<DomainException>(() => RoomId.Create(value!));
    }

    [Fact]
    public void Create_ValidValue_ReturnsRoomId()
    {
        var id = RoomId.Create("room1");

        Assert.Equal("room1", id.Value);
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var a = RoomId.Create("room1");
        var b = RoomId.Create("room1");

        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var a = RoomId.Create("room1");
        var b = RoomId.Create("room2");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var id = RoomId.Create("room1");

        Assert.Equal("room1", id.ToString());
    }
}
