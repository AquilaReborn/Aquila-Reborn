using Robust.Shared.GameObjects;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUCapsuleSphereComponent : Component
{
    [ViewVariables] public TimeSpan ExpiresAt;
    [DataField] public float Radius = 2f;
    [ViewVariables] public HashSet<EntityUid> TriggeredEntities = new();
    [ViewVariables] public int CapsulesDropped;
    [DataField] public int MaxCapsules = 5;
    [DataField] public string CapsuleProto = "WOUCapsule";
}