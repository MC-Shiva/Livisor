namespace Livisor.Server.Domain.ValueObject;

// "HH:mm:ss:ff"（時:分:秒:センチ秒）を表す値オブジェクト。
// 生成(Parse)時に妥当性を保証するため、これ以降は常に正しい時刻として扱える。
public readonly struct PlaybackTime
{
    public int Hours { get; }
    public int Minutes { get; }
    public int Seconds { get; }
    public int Centiseconds { get; }

    private PlaybackTime(int hours, int minutes, int seconds, int centiseconds)
    {
        Hours = hours;
        Minutes = minutes;
        Seconds = seconds;
        Centiseconds = centiseconds;
    }

    // 先頭アクション基準の相対再生などに使う総秒数。
    public double TotalSeconds => Hours * 3600 + Minutes * 60 + Seconds + Centiseconds / 100.0;

    // ワイヤ表記 "HH:mm:ss:ff" へ戻す。
    public string ToRawString() => $"{Hours:D2}:{Minutes:D2}:{Seconds:D2}:{Centiseconds:D2}";

    // "HH:mm:ss:ff" をパースする。不正なら DomainException。
    public static PlaybackTime Parse(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new DomainException("time must not be empty.");

        var parts = value.Split(':');
        if (parts.Length != 4
            || !int.TryParse(parts[0], out var h) || h is < 0 or > 23
            || !int.TryParse(parts[1], out var m) || m is < 0 or > 59
            || !int.TryParse(parts[2], out var s) || s is < 0 or > 59
            || !int.TryParse(parts[3], out var ff) || ff is < 0 or > 99)
        {
            throw new DomainException($"invalid time format: '{value}' (expected HH:mm:ss:ff).");
        }

        return new PlaybackTime(h, m, s, ff);
    }
}
