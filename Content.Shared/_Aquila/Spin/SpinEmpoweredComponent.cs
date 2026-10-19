using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Aquila.Spin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpinEmpoweredComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField]
    public float OriginalStaminaDamage;

    [DataField]
    public DamageSpecifier? OriginalHitDamage;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Effect;
}
