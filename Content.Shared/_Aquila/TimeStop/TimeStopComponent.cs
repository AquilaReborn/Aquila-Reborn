using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.Aquila.TimeStop;

[RegisterComponent, NetworkedComponent]
public sealed partial class TimeStopComponent : Component
{
    [DataField]
    public float Radius = 12.5f;

    [DataField]
    public float Duration = 10f;

    [DataField]
    public EntProtoId ActionId = "ActionAquilaTimeStop";

    [ViewVariables]
    public EntityUid? ActionEntity;

    [ViewVariables]
    public TimeSpan? ExpiresAt;

    [DataField]
    public float MobSpeedMultiplier = 0.0f;

    [DataField]
    public float ProjectileSpeedMultiplier = 0.0f;

    [DataField]
    public float FlashDuration = 1.0f;

    [DataField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/_Goobstation/Misc/sande_start.ogg");

    [DataField]
    public SoundSpecifier? EndSound = new SoundPathSpecifier("/Audio/_Goobstation/Misc/sande_end.ogg");

    [DataField]
    public SoundSpecifier? TickSound = new SoundPathSpecifier("/Audio/_Goobstation/Misc/sande_loop.ogg");

    [DataField]
    public float TickInterval = 1f;

    [ViewVariables]
    public EntityUid? TickStream;

    public TimeSpan NextTickSoundAt;

    public HashSet<EntityUid> InRange = new();
}
