using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankFineRevokeMessageEvent(int id) : CartridgeMessageEvent
{
    public readonly int Id = id;
}
