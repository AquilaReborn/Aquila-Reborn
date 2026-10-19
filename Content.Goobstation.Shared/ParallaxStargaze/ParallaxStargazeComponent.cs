using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.ParallaxStargaze;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Aquila AutoGenerateComponentState
public sealed partial class ParallaxStargazeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ActivationRadius = 5f;

    [DataField, AutoNetworkedField]
    public float StillTime = 6f;

    [DataField, AutoNetworkedField]
    public float FadeTime = 2.5f;

    [DataField, AutoNetworkedField]
    public float MobRadius = 5f;

    [DataField, AutoNetworkedField]
    public float MoveThreshold = 0.1f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Music = new SoundCollectionSpecifier("AmbienceSpaceChill");
}
