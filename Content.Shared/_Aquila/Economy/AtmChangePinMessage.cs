using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy;

[Serializable, NetSerializable]
public sealed class AtmChangePinMessage(int oldPin, int newPin) : BoundUserInterfaceMessage
{
    public readonly int OldPin = oldPin;
    public readonly int NewPin = newPin;
}
