using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class AtmWithdrawMessage(int amount) : BoundUserInterfaceMessage
{
    public readonly int Amount = amount;
}
