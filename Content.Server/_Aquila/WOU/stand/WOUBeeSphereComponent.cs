using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUBeeSphereComponent : Component
{
    [ViewVariables] public TimeSpan ExpiresAt;
    [DataField] public float Radius = 4f;
    [ViewVariables] public HashSet<EntityUid> TriggeredEntities = new();
    [ViewVariables] public int BeesSpawned;
    [DataField] public int MaxBees = 15;
    [DataField] public string BeeProto = "WOUBeeLicoxideAggressive";
}