using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankDepositBudgetMessageEvent(int amount) : CartridgeMessageEvent
{
    public readonly int Amount = amount;
}
