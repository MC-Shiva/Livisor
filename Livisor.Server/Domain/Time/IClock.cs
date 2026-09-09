namespace Livisor.Server.Domain.Time;

// 現在時刻の取得を抽象化するポート（依存性逆転の境界）。実装は Infrastructure 層に置く。
// サーバー時刻は再生開始の基準そのもの（業務上の意味を持つ値）なので、
// 技術詳細として外側に隠さず、Domain が定義した抽象として内側から扱う。
public interface IClock
{
    // 実時間（UTC）の Unix ミリ秒。
    long UtcNowUnixMs { get; }
}
