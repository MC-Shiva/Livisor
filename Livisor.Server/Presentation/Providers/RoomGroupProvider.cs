using Cysharp.Runtime.Multicast;
using Livisor.Server.Domain.ValueObject;
using Livisor.Shared.DTO;
using Livisor.Shared.Hubs;

namespace Livisor.Server.Presentation.Providers;

// room ごとの配信グループを、StreamingHub の外（Unary サービス）からも使えるようにする。
// MagicOnion が Hub に用意するグループは Hub インスタンスからしか触れないため、
// アプリケーション側で管理するグループ（Multicaster）を Singleton で共有する。
// アプリ管理グループはメンバーが 0 になっても自動では解放されないため、参加・離脱・配信を
// すべてこのクラスに集約し、グループの寿命と「確定と配信の順序」をここが一手に持つ。
public sealed class RoomGroupProvider
{
    // 他のグループ名と衝突しないよう、この用途であることを名前で示す。
    private const string GroupNamePrefix = "RoomState/";

    private readonly IMulticastGroupProvider _provider;

    // グループの生成・破棄と配信を直列化する。room ごとのロックにしないのは、
    // ロックを捨てた直後に別スレッドが新しいロックを作ると排他が壊れるため。
    private readonly object _gate = new();

    // 生存しているグループだけを持つ。Dispose 済みのものは載せない。
    private readonly Dictionary<string, IMulticastSyncGroup<Guid, IRoomStateHubReceiver>> _groups = new();

    public RoomGroupProvider(IMulticastGroupProvider provider) => _provider = provider;

    // 診断用。Leave で 0 に戻ることの確認に使う。
    public int ActiveGroupCount
    {
        get { lock (_gate) return _groups.Count; }
    }

    // room に参加し、参加時点の状態を同じ区間で読み取って返す。
    // Multicaster は同一キーを List で保持するため、同じ接続が二度 Add されると配信が二重に届く。
    // 追加の前に必ず同じキーを外す。
    // 参加してからロックの外で読むと、読み取りと応答の間に届いた配信のほうが先にクライアントへ着き、
    // あとから届いた古い値で上書きされる。参加と読み取りを分けないことでこれを防ぐ。
    public RoomStatePatch Join(RoomId roomId, Guid connectionId, IRoomStateHubReceiver receiver, Func<RoomStatePatch> snapshot)
    {
        var name = GroupName(roomId);
        lock (_gate)
        {
            if (!_groups.TryGetValue(name, out var group))
            {
                group = _provider.GetOrAddSynchronousGroup<Guid, IRoomStateHubReceiver>(name);
                _groups[name] = group;
            }

            group.Remove(connectionId);
            group.Add(connectionId, receiver);

            return snapshot();
        }
    }

    // room から離脱する。最後の1人が抜けたらグループ自体を捨てる。
    // Multicaster はメンバー 0 では解放せず、Dispose のときだけ provider の管理から外れる。
    public void Leave(RoomId roomId, Guid connectionId)
    {
        var name = GroupName(roomId);
        lock (_gate)
        {
            if (!_groups.TryGetValue(name, out var group))
                return;

            group.Remove(connectionId);
            if (group.Count() != 0)
                return;

            group.Dispose();
            _groups.Remove(name);
        }
    }

    // 状態の確定(commit)と配信(send)を同じ区間で行い、確定順と配信順を一致させる。
    // 参加者が 1 人もいない room ではグループを作らない（Unary を叩くだけで増えないようにする）。
    // 確定処理は参加者の有無にかかわらず必ず走る。
    // commit と send は _gate の内側で走る。_gate は room ごとではなく全体で 1 つなので、
    // ある room の配信が終わるまで他の room の Publish も待つ。
    // commit に渡してよいのは IRoomCache.Update のような短い処理だけで、IO や待ちは入れない。
    public RoomStatePatch PublishState(RoomId roomId, Func<RoomStatePatch> commit)
        => Publish(roomId, commit, static (receiver, patch) => receiver.OnStateChanged(patch));

    public TransportState PublishTransport(RoomId roomId, Func<TransportState> commit)
        => Publish(roomId, commit, static (receiver, state) => receiver.OnTransportChanged(state));

    private TResult Publish<TResult>(RoomId roomId, Func<TResult> commit, Action<IRoomStateHubReceiver, TResult> send)
    {
        var name = GroupName(roomId);
        lock (_gate)
        {
            var result = commit();
            if (_groups.TryGetValue(name, out var group))
                send(group.All, result);

            return result;
        }
    }

    private static string GroupName(RoomId roomId) => GroupNamePrefix + roomId.Value;
}
