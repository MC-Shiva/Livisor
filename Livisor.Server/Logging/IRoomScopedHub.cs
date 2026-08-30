using Livisor.Server.Domain.ValueObject;

namespace Livisor.Server.Logging;

// room に属する Hub の目印。HubLoggingFilter が、呼び出し時点で確定済みの RoomId を
// ログスコープへ載せるために参照する。
internal interface IRoomScopedHub
{
    // まだ参加していなければ null。
    RoomId? RoomId { get; }
}
