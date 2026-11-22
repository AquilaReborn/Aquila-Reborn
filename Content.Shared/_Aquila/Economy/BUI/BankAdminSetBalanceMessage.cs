using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankAdminSetBalanceMessage(int number, int balance, string comment) : EuiMessageBase
{
    public readonly int Number = number;
    public readonly int Balance = balance;
    public readonly string Comment = comment;
}
