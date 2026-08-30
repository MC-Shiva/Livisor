using Cysharp.Runtime.Multicast.InMemory;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Presentation.Providers;
using Livisor.Shared.DTO;
using Livisor.Shared.Hubs;

namespace Livisor.Server.Tests.Presentation;

public class RoomGroupProviderTests
{
    private sealed class SpyReceiver : IRoomStateHubReceiver
    {
        public int StateCount;
        public int TransportCount;

        public void OnStateChanged(RoomStatePatch patch) => StateCount++;

        public void OnTransportChanged(TransportState state) => TransportCount++;
    }

    private static RoomGroupProvider Create()
        => new(new InMemoryGroupProvider(DynamicInMemoryProxyFactory.Instance));

    // 参加時のスナップショットはこのテストの関心ではないため、空の差分を返す。
    private static void Join(RoomGroupProvider groups, RoomId room, Guid connectionId, IRoomStateHubReceiver receiver)
        => groups.Join(room, connectionId, receiver, static () => new RoomStatePatch());

    private static void PublishTransport(RoomGroupProvider groups, RoomId room)
        => groups.PublishTransport(room, static () => new TransportState());

    [Fact]
    public void Join_SameRoomTwice_DeliversOnce()
    {
        // Multicaster は同一キーを List で保持するため、Add を重ねると配信が二重に届く。
        var groups = Create();
        var room = RoomId.Create("room1");
        var connectionId = Guid.NewGuid();
        var receiver = new SpyReceiver();

        Join(groups, room, connectionId, receiver);
        Join(groups, room, connectionId, receiver);
        PublishTransport(groups, room);

        Assert.Equal(1, receiver.TransportCount);
    }

    [Fact]
    public void Join_ReturnsSnapshotTakenWhileJoining()
    {
        // 参加とスナップショットを分けると、読み取りより先に届いた配信を古い値で上書きしてしまう。
        var groups = Create();
        var room = RoomId.Create("room1");
        var expected = new RoomStatePatch { ServerTimeMs = 1_700_000_000_000 };

        var actual = groups.Join(room, Guid.NewGuid(), new SpyReceiver(), () => expected);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Leave_LastMember_ReleasesGroup()
    {
        var groups = Create();
        var room = RoomId.Create("room1");
        var connectionId = Guid.NewGuid();
        var receiver = new SpyReceiver();

        Join(groups, room, connectionId, receiver);
        Assert.Equal(1, groups.ActiveGroupCount);

        groups.Leave(room, connectionId);

        Assert.Equal(0, groups.ActiveGroupCount);
        PublishTransport(groups, room);
        Assert.Equal(0, receiver.TransportCount);
    }

    [Fact]
    public void Leave_OtherMemberRemains_KeepsGroup()
    {
        var groups = Create();
        var room = RoomId.Create("room1");
        var staying = new SpyReceiver();
        var leaving = new SpyReceiver();
        var leavingId = Guid.NewGuid();

        Join(groups, room, Guid.NewGuid(), staying);
        Join(groups, room, leavingId, leaving);
        groups.Leave(room, leavingId);

        Assert.Equal(1, groups.ActiveGroupCount);
        PublishTransport(groups, room);
        Assert.Equal(1, staying.TransportCount);
        Assert.Equal(0, leaving.TransportCount);
    }

    [Fact]
    public void PublishTransport_NoMember_DoesNotCreateGroup()
    {
        // Unary は Hub 参加者ゼロでも呼べる。ここでグループを作ると room 名ごとに増え続ける。
        var groups = Create();
        var room = RoomId.Create(Guid.NewGuid().ToString());

        var committed = 0;
        groups.PublishTransport(room, () => { committed = 42; return new TransportState(); });

        Assert.Equal(42, committed); // 参加者ゼロでも確定処理は必ず走る
        Assert.Equal(0, groups.ActiveGroupCount);
    }

    [Fact]
    public void PublishTransport_OtherRoom_DoesNotDeliver()
    {
        var groups = Create();
        var joined = RoomId.Create("room1");
        var other = RoomId.Create("room2");
        var receiver = new SpyReceiver();

        Join(groups, joined, Guid.NewGuid(), receiver);
        PublishTransport(groups, other);

        Assert.Equal(0, receiver.TransportCount);
    }

    [Fact]
    public void PublishState_DeliversToMembers()
    {
        var groups = Create();
        var room = RoomId.Create("room1");
        var receiver = new SpyReceiver();

        Join(groups, room, Guid.NewGuid(), receiver);
        groups.PublishState(room, static () => new RoomStatePatch());

        Assert.Equal(1, receiver.StateCount);
        Assert.Equal(0, receiver.TransportCount);
    }
}
