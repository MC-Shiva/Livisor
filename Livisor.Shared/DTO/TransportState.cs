using MessagePack;

namespace Livisor.Shared.DTO
{
    /// <summary>
    /// 再生トランスポートの現在値。
    /// <c>Playing</c> が true のときだけ、クライアントは次の式で予約アクションの発火時刻を求める。
    ///   発火するクライアント時刻 = StartedAtServerMs + 相対時間 + (クライアント時刻 - サーバー時刻)
    /// 右辺の括弧はクロック差で、<c>ITimelineService.GetServerTimeAsync</c> で測る。
    /// <c>Playing</c> が false のときは基準時刻が無いため、この式は成立しない。
    /// 停止中も <c>ScheduledAction</c> は残る（再生し直せば同じ相対位置で発火する）ので、
    /// 受信側は Playing が false になった時点で予約タイマーを取り消し、
    /// 次に Playing が true になったときに張り直す。
    /// </summary>
    [MessagePackObject]
    public class TransportState
    {
        /// <summary>再生中かどうか。</summary>
        [Key(0)]
        public bool Playing { get; set; }

        /// <summary>
        /// 再生を開始したサーバー時刻（UTC ミリ秒）。
        /// 停止中は 0。基準を持たないという意味であり、発火時刻の計算には使わない。
        /// </summary>
        [Key(1)]
        public long StartedAtServerMs { get; set; }

        /// <summary>
        /// この状態を確定した時点のサーバー時刻（UTC ミリ秒）。送信時刻にあたる。
        /// 受信したクライアントは、自分の時刻との差からクロック差を見積もれる。
        /// 再生位置（0:00 を再生開始とした相対時間）は <c>ServerTimeMs - StartedAtServerMs</c> で求まるため、
        /// 再生位置そのものは持たない。
        /// </summary>
        [Key(2)]
        public long ServerTimeMs { get; set; }

        /// <summary>
        /// 予約中のアクション。最大 1 件で、無ければ null。
        /// <c>Time</c> は絶対時刻ではなく、再生開始からの相対時間を表す。
        /// </summary>
        [Key(3)]
        public TimelineAction? ScheduledAction { get; set; }
    }
}
