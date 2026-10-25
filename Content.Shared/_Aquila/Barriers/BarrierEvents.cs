using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.Barriers;

public sealed partial class BarrierSpawnActionEvent : WorldTargetActionEvent
{
    [DataField(required: true)]
    public EntProtoId SpawnedEntityPrototype;
}

[Serializable, NetSerializable]
public sealed class BarrierOrientationToggleEvent : EntityEventArgs
{
    public NetEntity Action;
}
