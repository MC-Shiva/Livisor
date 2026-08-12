namespace Livisor.Server.Domain;

// ドメインの不変条件違反を表す例外。
// Presentation 層で gRPC のエラー(ReturnStatusException)へ変換する。
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
