using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Cyberware.Components;

/// <summary>
/// Импланты этого существа заглушены блокиратором: их действия не работают, а кибернагрузка снижена.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CyberwareBlockSystem))]
public sealed partial class CyberwareBlockedComponent : Component
{
    /// <summary>
    /// Во сколько раз снижается кибернагрузка.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LoadMultiplier = 0.5f;

    /// <summary>
    /// Блокиратор, который глушит импланты.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Blocker;
}
