using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Aquila.TimeStop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TimeStopSlowedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Source;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 0.2f;

    [DataField, AutoNetworkedField]
    public Vector2 OriginalLinearVelocity;

    [DataField, AutoNetworkedField]
    public float OldAngularVelocity;

    [DataField, AutoNetworkedField]
    public bool HadCollisionWake;

    [DataField, AutoNetworkedField]
    public bool IsSlowed = true;

    [DataField, AutoNetworkedField]
    public float RemainingTime;

    [DataField, AutoNetworkedField]
    public bool Grayscale;

    [DataField, AutoNetworkedField]
    public TimeSpan FlashEndTime;

    [DataField, AutoNetworkedField]
    public float FlashDuration = 1.0f;
}
