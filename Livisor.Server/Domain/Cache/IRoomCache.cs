using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Domain.Cache;

// room（Room集約）をメモリに保持するキャッシュのポート（依存性逆転の境界）。
// 実装は Infrastructure 層に置く。
public interface IRoomCache
{
    // Room を返す。無ければ初期状態（停止中・未予約・状態なし）の Room。
    Room Get(RoomId roomId);

    // Room を更新し、更新後の Room を返す。room が無ければ初期状態から作って更新する。
    // 更新は不変な Room の差し替えで表す。同時呼び出しでは update が再試行されうるため、
    // update には副作用を持たせない。
    Room Update(RoomId roomId, Func<Room, Room> update);

    void Remove(RoomId roomId);
}
