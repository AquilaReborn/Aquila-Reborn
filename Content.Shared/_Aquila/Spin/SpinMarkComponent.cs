using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.Spin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpinMarkComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public EntityUid? OwnerEntity;

    [DataField]
    public TimeSpan TickInterval = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan NextTickTime;

    [DataField]
    public float StaminaDamagePerTick = 10f;

    [DataField]
    public float AoeRadius = 1.5f;

    [DataField]
    public EntProtoId TickEffectProto = "SpinMarkTickEffect";
}
