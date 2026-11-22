using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankAdminRevertMessage(int number, TimeSpan time, int amount, string comment) : EuiMessageBase
{
    public readonly int Number = number;
    public readonly TimeSpan Time = time;
    public readonly int Amount = amount;
    public readonly string Comment = comment;
}
