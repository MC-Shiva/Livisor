using System.Collections.ObjectModel;
using Livisor.Shared.Common;

namespace Livisor.Server.Domain.ValueObject;

// room の同期状態。項目名と値の組を保持する不変のマップ。
// 同期する項目は公演ごとに増える（心拍数・音量・照明色 ...）ため、固定のフィールドではなく
// 可変のキーで持ち、既知のキーだけ値の種類を検証する。
// 全量と差分の両方をこの型で表す。差分として渡す側は引数名を patch に統一する。
// 「差分に無いキーは元の値を保つ」ことは Merge が保証する。
public sealed class RoomState
{
    public static RoomState Empty { get; } = new(new Dictionary<string, ActionValue>());

    private readonly Dictionary<string, ActionValue> _entries;

    // 内部の Dictionary をそのまま返すと、呼び出し側が IDictionary へキャストして書き換えられる。
    // 共有インスタンスの Empty が書き換わると全 room の初期状態が汚れるため、読み取り専用で包む。
    private readonly ReadOnlyDictionary<string, ActionValue> _readOnlyEntries;

    public IReadOnlyDictionary<string, ActionValue> Entries => _readOnlyEntries;

    private RoomState(Dictionary<string, ActionValue> entries)
    {
        _entries = entries;
        _readOnlyEntries = new ReadOnlyDictionary<string, ActionValue>(entries);
    }

    // 項目の並びから作る。同じキーが重複した場合は後の値を採る。
    public static RoomState Create(IReadOnlyList<KeyValuePair<string, ActionValue>> entries)
    {
        if (entries is null)
            throw new DomainException("state entries must not be null.");

        var map = new Dictionary<string, ActionValue>(entries.Count);
        foreach (var entry in entries)
        {
            Validate(entry.Key, entry.Value);
            map[entry.Key] = entry.Value;
        }

        return new RoomState(map);
    }

    // 差分を重ねた新しい状態を返す。差分に無いキーは元の値を保つ。
    public RoomState Merge(RoomState patch)
    {
        if (patch is null)
            throw new DomainException("state patch must not be null.");

        if (patch._entries.Count == 0)
            return this;

        var map = new Dictionary<string, ActionValue>(_entries);
        foreach (var entry in patch._entries)
            map[entry.Key] = entry.Value;

        return new RoomState(map);
    }

    // 既知のキーだけ値の種類を固定する。未知のキーは可変項目として受け入れる。
    // 値の範囲(音量の上下限など)はまだ決まっていないため、ここでは検証しない。
    private static void Validate(string key, ActionValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("state key must not be empty.");

        var expected = key switch
        {
            RoomStateKeys.Volume => ActionValueKind.Number,
            RoomStateKeys.HeartRate => ActionValueKind.Number,
            _ => (ActionValueKind?)null,
        };

        if (expected is not null && value.Kind != expected)
            throw new DomainException($"state '{key}' must be {expected}, but was {value.Kind}.");
    }
}
