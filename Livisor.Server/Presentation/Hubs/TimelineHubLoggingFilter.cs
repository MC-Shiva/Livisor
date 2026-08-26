using Livisor.Server.Logging;
using MagicOnion.Server.Hubs;
using Microsoft.Extensions.DependencyInjection;

namespace Livisor.Server.Presentation.Hubs;

// TimelineHub の全メソッド呼び出しに ConnectionId(・呼び出し時点で確定済みなら RoomId)を
// ログスコープとして自動付与する。OnConnected/OnDisconnected はこのフィルタを通らないため対象外。
// 将来的にHubが増えた場合はさらに汎化させるかも
public class TimelineHubLoggingFilterAttribute : StreamingHubFilterAttribute
{
    public override async ValueTask Invoke(StreamingHubContext context, Func<StreamingHubContext, ValueTask> next)
    {
        var logger = context.ServiceContext.ServiceProvider.GetRequiredService<ILogger<TimelineHub>>();
        var roomId = (context.HubInstance as TimelineHub)?.RoomId;

        using var scope = roomId is null
            ? logger.LogScope(("ConnectionId", context.ConnectionId))
            : logger.LogScope(("ConnectionId", context.ConnectionId), ("RoomId", roomId.Value));

        await next(context);
    }
}
