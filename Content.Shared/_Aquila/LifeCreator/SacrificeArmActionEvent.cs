using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.LifeCreator;

public sealed partial class SacrificeArmActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public EntProtoId MobPrototype;
}
