using Robust.Shared.GameStates;

namespace Content.Shared.Aquila.Spin;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpinEmpowerCastingComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public int Step;

    [ViewVariables(VVAccess.ReadOnly)]
    public ushort DoAfterId;
}
