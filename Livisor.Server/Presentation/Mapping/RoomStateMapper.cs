using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;

namespace Livisor.Server.Presentation.Mapping;

// Shared.DTO ↔ Domain の相互変換（同期状態）。DTO 依存を Presentation 境界に閉じ込める。
public static class RoomStateMapper
{
    // 受信 DTO → ドメイン。空のキーや既知キーの型違いはここで DomainException となる。
    public static RoomState ToDomain(RoomStateEntry[] entries)
    {
        if (entries is null)
            throw new DomainException("state entries must not be null.");

        var pairs = new List<KeyValuePair<string, ActionValue>>(entries.Length);
        foreach (var entry in entries)
        {
            if (entry is null)
                throw new DomainException("state entry must not be null.");

            pairs.Add(new KeyValuePair<string, ActionValue>(entry.Key, entry.Value));
        }

        return RoomState.Create(pairs);
    }

    // ドメイン → 配信 DTO。
    public static RoomStatePatch ToDto(RoomState state, long serverTimeMs)
    {
        var entries = new RoomStateEntry[state.Entries.Count];
        var index = 0;
        foreach (var entry in state.Entries)
            entries[index++] = new RoomStateEntry { Key = entry.Key, Value = entry.Value };

        return new RoomStatePatch { Entries = entries, ServerTimeMs = serverTimeMs };
    }
}
