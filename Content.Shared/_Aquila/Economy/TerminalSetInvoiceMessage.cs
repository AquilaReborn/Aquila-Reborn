using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy;

[Serializable, NetSerializable]
public sealed class TerminalSetInvoiceMessage(int amount, string purpose) : BoundUserInterfaceMessage
{
    public readonly int Amount = amount;
    public readonly string Purpose = purpose;
}
