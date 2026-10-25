using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.LifeCreator;

[Serializable, NetSerializable]
public sealed partial class VivifyItemDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public EntProtoId CreaturePrototype;

    private VivifyItemDoAfterEvent() { }

    public VivifyItemDoAfterEvent(EntProtoId creaturePrototype)
    {
        CreaturePrototype = creaturePrototype;
    }

    public override DoAfterEvent Clone() => this;
}
