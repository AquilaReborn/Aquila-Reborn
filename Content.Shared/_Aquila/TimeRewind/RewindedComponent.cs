using Robust.Shared.GameStates;

namespace Content.Shared.Aquila.TimeRewind;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RewindedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration;
}
