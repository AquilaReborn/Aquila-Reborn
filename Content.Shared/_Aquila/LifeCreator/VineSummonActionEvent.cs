using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.LifeCreator;

public sealed partial class VineSummonActionEvent : WorldTargetActionEvent
{
    [DataField(required: true)]
    public EntProtoId VinePrototype;

    [DataField]
    public int Radius = 3;

    [DataField]
    public TimeSpan SpawnDelayPerTile = TimeSpan.FromSeconds(0.15);
}
