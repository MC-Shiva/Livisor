namespace Livisor.Server.Logging;

// 構造化フィールドをタプルで渡すだけで、Messageに埋め込まず出力できるようにする薄いラッパー。
// C#の拡張メソッドを使っている
// 参考:https://ufcpp.net/study/csharp/sp3_extension.html
public static class LoggerExtensions
{
    public static void LogInfo(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        => Log(logger, LogLevel.Information, message, fields);

    public static void LogWarn(this ILogger logger, string message, params (string Key, object? Value)[] fields)
        => Log(logger, LogLevel.Warning, message, fields);

    // Hubフィルタ等、ログ出力と切り離してスコープだけ開きたい場合に使う。
    public static IDisposable? LogScope(this ILogger logger, params (string Key, object? Value)[] fields)
        => fields.Length == 0 ? null : logger.BeginScope(fields.ToDictionary(f => f.Key, f => f.Value));

    private static void Log(ILogger logger, LogLevel level, string message, (string Key, object? Value)[] fields)
    {
        using var scope = logger.LogScope(fields);
        logger.Log(level, message);
    }
}
