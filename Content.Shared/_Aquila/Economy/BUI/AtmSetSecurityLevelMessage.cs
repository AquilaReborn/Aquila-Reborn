using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class AtmSetSecurityLevelMessage(int level) : BoundUserInterfaceMessage
{
    public readonly int Level = level;
}
