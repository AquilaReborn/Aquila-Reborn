using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUBeeSphereComponent : Component
{
    [DataField] public TimeSpan Lifetime = TimeSpan.FromSeconds(5);
    [DataField] public float Radius = 4f;
    [DataField] public int MaxBees = 15;
    [DataField] public EntProtoId BeeProto = "WOUBeeLicoxideAggressive";
    [DataField] public float BeeLifetime = 10f;

    [ViewVariables] public TimeSpan ExpiresAt;
    [ViewVariables] public HashSet<EntityUid> TriggeredEntities = new();
    [ViewVariables] public int BeesSpawned;
}