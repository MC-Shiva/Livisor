using System;
using MessagePack;

namespace Livisor.Shared.DTO
{
    /// <summary>
    /// <see cref="ActionValue"/> が保持する値の種類。
    /// </summary>
    public enum ActionValueKind
    {
        Number,
        Bool,
        Text,
    }

    /// <summary>
    /// タイムラインアクションに付随する値。数値 / 真偽値 / 文字列のいずれか 1 つを保持する。
    /// ワイヤ上（MessagePack）では <see cref="ActionValueFormatter"/> により生プリミティブとして
    /// やり取りする（例: 1 / true / "intro"）。
    /// 例: { "action": { "start": 1 } } / { "action": { "start": true } } / { "action": { "cue": "intro" } }
    /// </summary>
    [MessagePackFormatter(typeof(ActionValueFormatter))]
    public readonly struct ActionValue : IEquatable<ActionValue>
    {
        public ActionValueKind Kind { get; }
        public int Number { get; }
        public bool Bool { get; }
        public string Text { get; }

        private ActionValue(ActionValueKind kind, int number, bool boolValue, string text)
        {
            Kind = kind;
            Number = number;
            Bool = boolValue;
            Text = text;
        }

        public static ActionValue From(int value) => new(ActionValueKind.Number, value, default, string.Empty);

        public static ActionValue From(bool value) => new(ActionValueKind.Bool, default, value, string.Empty);

        public static ActionValue From(string? value) => new(ActionValueKind.Text, default, default, value ?? string.Empty);

        public static implicit operator ActionValue(int value) => From(value);

        public static implicit operator ActionValue(bool value) => From(value);

        public static implicit operator ActionValue(string value) => From(value);

        public override string ToString() => Kind switch
        {
            ActionValueKind.Number => Number.ToString(),
            ActionValueKind.Bool => Bool.ToString(),
            ActionValueKind.Text => Text,
            _ => string.Empty,
        };

        public bool Equals(ActionValue other) => Kind == other.Kind
            && Number == other.Number
            && Bool == other.Bool
            && Text == other.Text;

        public override bool Equals(object obj) => obj is ActionValue other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Kind, Number, Bool, Text);
    }
}
