using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankAdminResetPinMessage(int number) : EuiMessageBase
{
    public readonly int Number = number;
}
