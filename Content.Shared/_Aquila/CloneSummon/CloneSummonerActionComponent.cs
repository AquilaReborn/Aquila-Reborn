using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.Clone;

[RegisterComponent]
public sealed partial class CloneSummonerActionComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionSummonClone";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public EntProtoId? SpawnEffect = "SpawnLightEffect";
}
