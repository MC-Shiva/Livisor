using Livisor.Server.Domain.Entity;

namespace Livisor.Server.Domain.Cache;

// room ごとのタイムライン履歴をメモリに保持するキャッシュのポート（依存性逆転の境界）。
// 実装は Infrastructure 層に置く。
public interface ITimelineCache
{
    // タイムラインを蓄積する（上書きせず追記）。
    void Add(string roomId, Timeline timeline);

    // 蓄積済みのタイムライン一覧を返す。なければ空リスト。
    IReadOnlyList<Timeline> GetAll(string roomId);
    
    void RemoveAll(string roomId);
}
