using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.Common;

namespace Livisor.Server.Tests.Domain;

public class PlaybackTimeTests
{
    // --- Parse 正常系 ---

    [Fact]
    public void Parse_ValidString_ReturnsCorrectFields()
    {
        var t = PlaybackTime.Parse("10:30:45:50");
        Assert.Equal(10, t.Hours);
        Assert.Equal(30, t.Minutes);
        Assert.Equal(45, t.Seconds);
        Assert.Equal(50, t.Centiseconds);
    }

    [Fact]
    public void Parse_ZeroPadded_Parses()
    {
        var t = PlaybackTime.Parse("00:00:00:00");
        Assert.Equal(0, t.Hours);
        Assert.Equal(0, t.Minutes);
        Assert.Equal(0, t.Seconds);
        Assert.Equal(0, t.Centiseconds);
    }

    [Fact]
    public void Parse_BoundaryValues_Parses()
    {
        var t = PlaybackTime.Parse("23:59:59:99");
        Assert.Equal(23, t.Hours);
        Assert.Equal(59, t.Minutes);
        Assert.Equal(59, t.Seconds);
        Assert.Equal(99, t.Centiseconds);
    }

    // --- Parse 異常系 ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Parse_NullOrEmpty_ThrowsDomainException(string? value)
    {
        Assert.Throws<DomainException>(() => PlaybackTimeParser.Parse(value));
    }

    [Theory]
    [InlineData("10:30:45")]         // セグメント 3 つ
    [InlineData("10:30:45:50:00")]   // セグメント 5 つ
    [InlineData("10-30-45-50")]      // 区切り文字が違う
    public void Parse_WrongSegmentCount_ThrowsDomainException(string value)
    {
        Assert.Throws<DomainException>(() => PlaybackTimeParser.Parse(value));
    }

    [Theory]
    [InlineData("24:00:00:00")]  // HH 上限超過
    [InlineData("-1:00:00:00")]  // HH 負数
    [InlineData("aa:00:00:00")]  // HH 非数値
    [InlineData("00:60:00:00")]  // mm 上限超過
    [InlineData("00:-1:00:00")]  // mm 負数
    [InlineData("00:00:60:00")]  // ss 上限超過
    [InlineData("00:00:00:100")] // ff 上限超過
    [InlineData("00:00:00:-1")]  // ff 負数
    public void Parse_OutOfRangeField_ThrowsDomainException(string value)
    {
        Assert.Throws<DomainException>(() => PlaybackTimeParser.Parse(value));
    }

    // --- TryParse 異常系 ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("10:30:45")]
    [InlineData("24:00:00:00")]
    public void TryParse_InvalidValue_ReturnsFalse(string? value)
    {
        Assert.False(PlaybackTime.TryParse(value, out _));
    }

    // --- TotalSeconds:正常系 ---

    [Theory]
    [InlineData("00:00:00:00", 0.0)]
    [InlineData("01:00:00:00", 3600.0)]
    [InlineData("00:01:00:00", 60.0)]
    [InlineData("00:00:01:00", 1.0)]
    [InlineData("00:00:00:50", 0.5)]
    [InlineData("10:30:45:25", 10 * 3600 + 30 * 60 + 45 + 0.25)]
    public void TotalSeconds_ReturnsCorrectValue(string input, double expected)
    {
        var t = PlaybackTime.Parse(input);
        Assert.Equal(expected, t.TotalSeconds, precision: 10);
    }

    // --- ToRawString:正常系 ---
    // Parseされた後にしかStringにできない

    [Theory]
    [InlineData("10:05:03:07")]
    [InlineData("00:00:00:00")]
    [InlineData("23:59:59:99")]
    public void ToRawString_RoundTrip(string input)
    {
        var result = PlaybackTime.Parse(input).ToRawString();
        Assert.Equal(input, result);
    }
}
