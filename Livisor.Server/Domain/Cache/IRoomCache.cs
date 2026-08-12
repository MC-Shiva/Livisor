using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Domain.Cache;

// room（Room集約）をメモリに保持するキャッシュのポート（依存性逆転の境界）。
// 実装は Infrastructure 層に置く。
public interface IRoomCache
{
    // 現在配信中のタイムラインを差し替え、差し替え後の Room を返す。room がなければ作る。
    Room SetCurrentTimeline(RoomId roomId, Timeline timeline);

    // Room を返す。なければ Current が未設定の Room。
    Room Get(RoomId roomId);

    void Remove(RoomId roomId);
}
