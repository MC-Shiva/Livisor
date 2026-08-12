using Livisor.Shared.DTO;

namespace Livisor.Shared.Hubs
{
    /// <summary>
    /// サーバ → 配信対象クライアントへのコールバック（受信）契約。
    /// StreamingHub の受信側インターフェース。
    /// </summary>
    public interface ITimelineHubReceiver
    {
        /// <summary>タイムライン配列を受信したときに呼ばれる。<paramref name="broadcastAtMs"/> を基準に絶対時刻で再生する。</summary>
        void OnBroadcastTimeline(TimelineAction[] actions, long broadcastAtMs);
    }
}
