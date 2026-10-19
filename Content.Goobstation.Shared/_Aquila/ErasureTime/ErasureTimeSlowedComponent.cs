using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Aquila.ErasureTime;

[RegisterComponent, NetworkedComponent]
public sealed partial class ErasureTimeSlowedComponent : Component
{
    [DataField]
    public bool IsSlowed;

    [DataField]
    public bool OriginalDrawFov = true;

    [DataField]
    public EntityUid Source;

    [DataField]
    public float SpeedMultiplier = 1f;

    [DataField]
    public Vector2 OriginalLinearVelocity;
}
