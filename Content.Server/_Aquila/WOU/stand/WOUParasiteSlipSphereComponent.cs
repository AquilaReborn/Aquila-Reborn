using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUParasiteSlipSphereComponent : Component
{
    [ViewVariables] public TimeSpan ExpiresAt;
    [DataField] public float Radius = 1f;
    [ViewVariables] public HashSet<EntityUid> TriggeredEntities = new();
}