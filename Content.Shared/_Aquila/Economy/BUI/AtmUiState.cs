using Content.Shared._Aquila.Economy.Types;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class AtmUiState(int? number, string? ownerName, int balance, bool canOpen = false, bool offline = false, int securityLevel = BankSecurityLevels.Open) : BoundUserInterfaceState
{
    public readonly int? Number = number;
    public readonly string? OwnerName = ownerName;
    public readonly int Balance = balance;
    public readonly bool CanOpen = canOpen;
    public readonly bool Offline = offline;
    public readonly int SecurityLevel = securityLevel;
}
