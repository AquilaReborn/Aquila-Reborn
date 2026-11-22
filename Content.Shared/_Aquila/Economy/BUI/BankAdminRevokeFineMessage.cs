using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankAdminRevokeFineMessage(int id) : EuiMessageBase
{
    public readonly int Id = id;
}
