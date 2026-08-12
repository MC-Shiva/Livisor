using Livisor.Shared.Common;

namespace Livisor.Server.Domain.ValueObject;

// Shared の時刻ルール(PlaybackTime)を Domain の不変条件（DomainException）へ橋渡しする。
public static class PlaybackTimeParser
{
    public static PlaybackTime Parse(string value)
        => PlaybackTime.TryParse(value, out var result)
            ? result
            : throw new DomainException($"invalid time format: '{value}' (expected HH:mm:ss:ff).");
}
