using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankAdminAccountData(int number, string ownerName, string? jobTitle, int balance)
{
    public readonly int Number = number;
    public readonly string OwnerName = ownerName;
    public readonly string? JobTitle = jobTitle;
    public readonly int Balance = balance;
}
