using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.Clone;

[RegisterComponent, NetworkedComponent]
public sealed partial class CloneSummonComponent : Component
{
    [DataField]
    public EntityUid? Summoner;

    [DataField]
    public float TimeToLive = 30f;

    [DataField]
    public List<EntityUid> OriginalItems = new();

    [DataField]
    public EntProtoId? DespawnEffect;

    [ViewVariables]
    public bool Despawning;
}
