using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUParasiteSlipSphereComponent : Component
{
    [DataField] public TimeSpan Lifetime = TimeSpan.FromSeconds(3);
    [DataField] public float Radius = 1f;
    [DataField] public TimeSpan KnockdownTime = TimeSpan.FromSeconds(3);
    [DataField] public string DamageType = "Blunt";
    [DataField] public float DamageAmount = 40f;
    [DataField] public float StaminaDamage = 200f;
    [DataField] public float MinVelocitySquared = 0.01f;

    [ViewVariables] public TimeSpan ExpiresAt;
    [ViewVariables] public HashSet<EntityUid> TriggeredEntities = new();
}