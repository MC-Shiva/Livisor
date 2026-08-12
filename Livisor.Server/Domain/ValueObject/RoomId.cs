namespace Livisor.Server.Domain.ValueObject;

// room の識別子。空・空白のみは不正とする。
// struct ではなく class にするのは、default(RoomId) が Create の検証を迂回して
// Value == null の穴を作るのを避けるため。
public sealed class RoomId : IEquatable<RoomId>
{
    public string Value { get; }

    private RoomId(string value) => Value = value;

    public static RoomId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("roomId must not be empty.");

        return new RoomId(value);
    }

    public bool Equals(RoomId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as RoomId);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
