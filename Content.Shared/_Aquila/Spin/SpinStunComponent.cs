using Robust.Shared.GameStates;

namespace Content.Shared.Aquila.Spin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpinStunActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;
}
