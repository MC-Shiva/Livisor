using MessagePack;

namespace Livisor.Shared.DTO
{
    /// <summary>
    /// タイムライン上の 1 アクション。Issue #2 で確定した JSON 配列の 1 要素に対応する。
    /// 例: { "time": "10:00:00:00", "action": { "start": 1 } }
    /// </summary>
    [MessagePackObject]
    public class TimelineAction
    {
        /// <summary>実行時刻。Issue の表記 "HH:mm:ss:ff"（時:分:秒:フレーム/センチ秒）をそのまま保持する。</summary>
        [Key(0)]
        public string Time { get; set; } = string.Empty;

        /// <summary>操作の種類（start / stop / volumeChange）。</summary>
        [Key(1)]
        public ActionType Action { get; set; }

        /// <summary>操作に付随する値。start=1 / volumeChange=10 など。</summary>
        [Key(2)]
        public int Value { get; set; }
    }
}
