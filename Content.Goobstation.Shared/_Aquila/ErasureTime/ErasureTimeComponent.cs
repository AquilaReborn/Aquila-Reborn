using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Aquila.ErasureTime;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ErasureTimeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [ViewVariables]
    public bool CasterOriginalDrawFov = true;

    [DataField]
    public EntProtoId ActionId = "ActionErasureTime";

    [ViewVariables]
    public EntityUid? ActionEntity;

    [DataField]
    public float Duration = 12f;

    [DataField]
    public float WaveDuration = 1.5f;

    [DataField]
    public float FadeTime = 0.25f;

    [DataField]
    public float MobRadius = 12.5f;

    [DataField]
    public float MoveThreshold = 20f;

    [DataField]
    public SoundSpecifier Music = new SoundCollectionSpecifier("AmbienceSpaceChill");

    [DataField]
    public float MobSpeedMultiplier = 0.15f;

    [DataField]
    public float ProjectileSpeedMultiplier = 0.02f;

    [DataField]
    public float PlayerSpeedModifier = 1.3f;

    [ViewVariables]
    public TimeSpan EndTime;

    [DataField]
    public SoundSpecifier? ActivationSound = new SoundPathSpecifier("/Audio/_Aquila/TimeSkip/TimeSkipStart.ogg");

    [DataField]
    public SoundSpecifier? DeactivationSound = new SoundPathSpecifier("/Audio/_Aquila/TimeSkip/TimeSkipEnd.ogg");
}
