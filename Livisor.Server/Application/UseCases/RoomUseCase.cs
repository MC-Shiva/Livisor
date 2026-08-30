using Livisor.Server.Domain.Cache;
using Livisor.Server.Domain.Entity;
using Livisor.Server.Domain.Time;
using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Application.UseCases;

// room に対する操作をまとめたユースケース。
// どのメソッドも「キャッシュから Room を取り出し、Domain のルールに従って新しい Room を作り、書き戻す」
// という同じ流れなので、1つのクラスにまとめている。業務ルール自体は Room / Transport /
// ScheduledAction / RoomState が持ち、ここは流れを組み立てるだけで判断はしない。
//
// 呼び出し元は2つある。
//   - ITimelineService(Unary): Get / Play / Stop / Schedule / CancelSchedule
//   - IRoomStateHub(StreamingHub): Get / ApplyState
public sealed class RoomUseCase
{
    private readonly IRoomCache _cache;
    private readonly IClock _clock;

    public RoomUseCase(IRoomCache cache, IClock clock)
    {
        _cache = cache;
        _clock = clock;
    }

    // room の現在値を返す。room がまだ無ければ、何もしていない状態の Room を返す。
    public Room Get(RoomId roomId) => _cache.Get(roomId);

    // 再生を開始する。開始時刻はサーバーの実時間で確定させる。
    // この時刻は、配信時に「どれだけ再生が進んでいたか」を出すために使う（TransportState の式を参照）。
    public Room Play(RoomId roomId)
    {
        // 更新は競合すると再試行されるため、時刻はその外側で1回だけ読む。
        // 中で読むと、再試行のたびに開始時刻がずれてしまう。
        var startedAt = _clock.UtcNowUnixMs;
        return _cache.Update(roomId, room => room.Play(startedAt));
    }

    // 再生を停止する。予約は取り消さない（再生し直せば同じ相対位置で発火する）。
    public Room Stop(RoomId roomId) => _cache.Update(roomId, room => room.Stop());

    // 予約を1件だけ登録する。既にあれば置き換える。
    public Room Schedule(RoomId roomId, ScheduledAction action) => _cache.Update(roomId, room => room.Schedule(action));

    // 予約を取り消す。
    public Room CancelSchedule(RoomId roomId) => _cache.Update(roomId, room => room.CancelSchedule());

    // 状態の差分を重ねる。差分に無い項目は元の値のまま残る。
    public Room ApplyState(RoomId roomId, RoomState patch) => _cache.Update(roomId, room => room.ApplyState(patch));
}
