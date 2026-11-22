using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankAdminRequestReplyMessage(int id, bool approve) : EuiMessageBase
{
    public readonly int Id = id;
    public readonly bool Approve = approve;
}
