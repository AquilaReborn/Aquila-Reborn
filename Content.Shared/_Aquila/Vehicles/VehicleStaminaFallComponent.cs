using Robust.Shared.GameStates;
using Content.Shared.DoAfter;

namespace Content.Shared.Aquila.Vehicles;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleStaminaFallComponent : Component
{
    [DataField]
    public float StunDuration = 4f;

    [ViewVariables]
    public bool WasCritical;
}

