using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.PDA.Ringer;

/// <summary>
/// Просьба клиента включить или выключить беззвучный режим КПК.
/// </summary>
[Serializable, NetSerializable]
public sealed class RingerSetSilentMessage(bool silent) : BoundUserInterfaceMessage
{
    /// <summary>
    /// Нужно ли включить беззвучный режим.
    /// </summary>
    public bool Silent { get; } = silent;
}
