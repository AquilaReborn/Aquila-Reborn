using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy;

[Serializable, NetSerializable]
public sealed class PaymentTerminalUiState(string? linkedOwnerName, string? budgetName, int amount, string purpose, bool ready, bool isOwner, bool fineOnly, bool canLinkCard) : BoundUserInterfaceState
{
    public readonly string? LinkedOwnerName = linkedOwnerName;
    public readonly string? BudgetName = budgetName;
    public readonly int Amount = amount;
    public readonly string Purpose = purpose;
    public readonly bool Ready = ready;
    public readonly bool IsOwner = isOwner;
    public readonly bool FineOnly = fineOnly;
    public readonly bool CanLinkCard = canLinkCard;
}
