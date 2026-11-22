using Content.Shared._Aquila.Economy.DataDefinitions;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankUiState(
    int? number,
    string? ownerName,
    string? jobTitle,
    int balance,
    List<BankTransaction> history,
    BankManageUiState? manage = null,
    bool canOpen = false,
    bool offline = false)
    : BoundUserInterfaceState
{
    public readonly int? Number = number;
    public readonly string? OwnerName = ownerName;
    public readonly string? JobTitle = jobTitle;
    public readonly int Balance = balance;
    public readonly List<BankTransaction> History = history;
    public readonly BankManageUiState? Manage = manage;
    public readonly bool CanOpen = canOpen;
    public readonly bool Offline = offline;
}
