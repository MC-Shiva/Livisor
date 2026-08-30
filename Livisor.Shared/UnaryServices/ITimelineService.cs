using Livisor.Shared.DTO;
using MagicOnion;

namespace Livisor.Shared.UnaryServices
{
    /// <summary>
    /// タイムライン（再生トランスポートと予約アクション）の Unary 契約。
    /// 遅延なく確定させたい一度きりの操作（再生・停止・予約）をここで扱う。
    /// 変化し続ける値の同期は <c>Livisor.Shared.Hubs.IRoomStateHub</c> が担う。
    /// 確定した結果は応答で返すと同時に、同じ room の Hub 参加者へも通知される。
    /// </summary>
    public interface ITimelineService : IService<ITimelineService>
    {
        /// <summary>
        /// サーバーの現在時刻（UTC ミリ秒）。クライアントとのクロック差の測定に使う。
        /// クライアントもサーバーも NTP で時刻が合っている前提を置いているため、差は 1 回の呼び出しで求める
        /// （2026-08-29 の決定 / Issue #19）。
        ///
        /// この測定値には片道の通信遅延が混ざる。NTP が効いていれば時計のズレは 0 なので、
        /// 差はほぼ片道遅延そのものになり、それを足すと片道遅延ぶん遅れて発火する。
        /// 精度を詰めるなら、複数回呼んで往復時間が最小のサンプルを採り、その半分を引く方式へ変えられる。
        /// サーバーの実装とワイヤ契約は変えずに済み、変更はクライアント側だけで閉じる。
        /// </summary>
        UnaryResult<long> GetServerTimeAsync();

        /// <summary>現在のトランスポートを取得する。</summary>
        UnaryResult<TransportState> GetTransportAsync(string roomId);

        /// <summary>再生を開始する。開始時刻はサーバーが確定する。再生中の呼び出しは開始時刻を動かさない。</summary>
        UnaryResult<TransportState> PlayAsync(string roomId);

        /// <summary>再生を停止する。</summary>
        UnaryResult<TransportState> StopAsync(string roomId);

        /// <summary>
        /// 予約アクションを 1 件登録する。既存の予約は置き換える。
        /// <paramref name="action"/> の Time は再生開始からの相対時間として扱う。
        /// </summary>
        UnaryResult<TransportState> ScheduleActionAsync(string roomId, TimelineAction action);

        /// <summary>予約アクションを取り消す。</summary>
        UnaryResult<TransportState> CancelScheduledActionAsync(string roomId);
    }
}
