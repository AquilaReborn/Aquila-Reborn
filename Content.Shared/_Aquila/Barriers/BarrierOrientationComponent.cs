using Robust.Shared.GameStates;

namespace Content.Shared.Aquila.Barriers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BarrierOrientationComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsVertical = false;
}
