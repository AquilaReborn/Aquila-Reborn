using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankRequestReplyMessageEvent(int id, bool approve) : CartridgeMessageEvent
{
    public readonly int Id = id;
    public readonly bool Approve = approve;
}
