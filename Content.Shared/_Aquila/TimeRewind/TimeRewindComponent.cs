using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.TimeRewind;

[RegisterComponent]
public sealed partial class TimeRewindComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionTimeRewind";

    [ViewVariables]
    public EntityUid? ActionEntity;

    [DataField]
    public float Radius = 12.5f;

    [DataField]
    public TimeSpan RewindDelay = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(0.65);
}
