using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.PreCritical;

/// <summary>
/// Активный предкрит и рассчитанные параметры ползания.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(PreCriticalSystem))]
public sealed partial class PreCriticalActiveComponent : Component
{
    [AutoNetworkedField]
    public float SpeedModifier = 1f;

    [AutoNetworkedField]
    public float FrictionModifier = 1f;
}
