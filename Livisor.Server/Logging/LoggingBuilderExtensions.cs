using ZLogger;

namespace Livisor.Server.Logging;

// ロギングパイプラインの構成をここに閉じ込める。
public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddAppLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddZLoggerConsole(options =>
        {
            // BeginScope で渡した RoomId/ConnectionId 等を JSON のトップレベルに出力する。
            options.IncludeScopes = true;
            options.UseJsonFormatter();
        });
        return logging;
    }
}
