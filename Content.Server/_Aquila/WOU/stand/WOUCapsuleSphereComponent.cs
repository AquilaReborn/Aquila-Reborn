using Content.Shared.Explosion;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUCapsuleSphereComponent : Component
{
    [DataField] public TimeSpan Lifetime = TimeSpan.FromSeconds(4);
    [DataField] public float Radius = 2f;
    [DataField] public int MaxCapsules = 5;
    [DataField] public EntProtoId CapsuleProto = "WOUCapsule";

    [DataField] public TimeSpan TriggerDelay = TimeSpan.FromSeconds(1.5);
    [DataField] public ProtoId<ExplosionPrototype> ExplosionType = "Default";
    [DataField] public float TotalIntensity = 50f;
    [DataField] public float Slope = 1.5f;
    [DataField] public float MaxTileIntensity = 1f;
    [DataField] public float TileBreakScale = 0f;
    [DataField] public int MaxTileBreak = 0;
    [DataField] public bool CanCreateVacuum = false;

    [DataField] public TimeSpan RatSpawnDelay = TimeSpan.FromSeconds(1);
    [DataField] public int RatsPerCapsule = 2;
    [DataField] public EntProtoId RatProto = "WOUMobRatAggressive";
    [DataField] public float RatLifetime = 10f;

    [ViewVariables] public TimeSpan ExpiresAt;
    [ViewVariables] public HashSet<EntityUid> TriggeredEntities = new();
    [ViewVariables] public int CapsulesDropped;
}