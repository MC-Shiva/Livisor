using Livisor.Server.Logging;
using MagicOnion.Server.Hubs;
using Microsoft.Extensions.DependencyInjection;

namespace Livisor.Server.Presentation.Hubs;

// Hub の全メソッド呼び出しに ConnectionId(・呼び出し時点で確定済みなら RoomId)を
// ログスコープとして自動付与する。OnConnected/OnDisconnected はこのフィルタを通らないため対象外。
// ログのカテゴリは Hub の実型から作るため、Hub が増えてもそのまま使える。
public class HubLoggingFilterAttribute : StreamingHubFilterAttribute
{
    public override async ValueTask Invoke(StreamingHubContext context, Func<StreamingHubContext, ValueTask> next)
    {
        var loggerFactory = context.ServiceContext.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(context.HubInstance.GetType());
        var roomId = (context.HubInstance as IRoomScopedHub)?.RoomId;

        using var scope = roomId is null
            ? logger.LogScope(("ConnectionId", context.ConnectionId))
            : logger.LogScope(("ConnectionId", context.ConnectionId), ("RoomId", roomId.Value));

        await next(context);
    }
}
