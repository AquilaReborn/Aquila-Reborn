using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Cyberware.Hack;

/// <summary>
/// Идёт взлом: от этого существа к устройству тянется зелёное цифровое полотно.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CyberHackSystem))]
public sealed partial class CyberHackBeamComponent : Component
{
    /// <summary>
    /// Взламываемое устройство.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// Ширина полотна в тайлах.
    /// </summary>
    [DataField]
    public float Width = 0.6f;
}
