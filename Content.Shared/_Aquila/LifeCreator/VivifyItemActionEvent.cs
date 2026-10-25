using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.LifeCreator;

public sealed partial class VivifyItemActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public EntProtoId CreaturePrototype;
}
