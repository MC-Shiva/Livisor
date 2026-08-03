using System.Threading.Tasks;
using Livisor.Shared.DTO;
using MagicOnion;

namespace Livisor.Shared.Hubs
{
    /// <summary>
    /// タイムライン配信用の StreamingHub 契約。
    /// 配信元・配信対象の双方がこの Hub に接続し、同じ room に参加する。
    /// 配信元が <see cref="BroadcastTimelineAsync"/> を呼ぶと、サーバが同じ room の
    /// 配信対象へ <see cref="ITimelineHubReceiver.OnBroadcastTimeline"/> をプッシュする。
    /// </summary>
    public interface ITimelineHub : IStreamingHub<ITimelineHub, ITimelineHubReceiver>
    {
        /// <summary>指定した room（グループ）に参加する。</summary>
        ValueTask JoinAsync(string roomId);

        /// <summary>タイムライン配列を同じ room の配信対象へ一括配信する。</summary>
        ValueTask BroadcastTimelineAsync(TimelineAction[] actions);
    }
}
