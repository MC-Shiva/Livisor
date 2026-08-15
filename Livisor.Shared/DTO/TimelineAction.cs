using Livisor.Shared.Common;
using MessagePack;

namespace Livisor.Shared.DTO
{
    /// <summary>
    /// タイムライン上の 1 アクション。Issue #11 で確定した JSON 配列の 1 要素に対応する。
    /// 例: { "time": "10:00:00:00", "action": { "play": true } }
    /// 例: { "time": "10:00:00:00", "action": { "play": false } }
    /// 例: { "time": "10:00:00:00", "action": { "volumeChange": 10 } }
    /// </summary>
    [MessagePackObject]
    public class TimelineAction
    {
        /// <summary>実行時刻。Issue の表記 "HH:mm:ss:ff"（時:分:秒:フレーム/センチ秒）をそのまま保持する。</summary>
        [Key(0)]
        public string Time { get; set; } = string.Empty;

        /// <summary>操作の種類（play / volumeChange）。</summary>
        [Key(1)]
        public ActionType Action { get; set; }

        /// <summary>操作に付随する値。play=true（再生）/ play=false（停止）/ volumeChange=10 など。</summary>
        [Key(2)]
        [MessagePackFormatter(typeof(ActionValueFormatter))]
        public ActionValue Value { get; set; }
    }
}
