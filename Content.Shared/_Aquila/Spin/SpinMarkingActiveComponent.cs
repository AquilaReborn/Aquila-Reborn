using Robust.Shared.GameStates;

namespace Content.Shared.Aquila.Spin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpinMarkingActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;
}
