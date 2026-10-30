using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.HolyCorpse;

[Serializable, NetSerializable]
public sealed partial class HolyCorpseImplantDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetEntity? FlashEntity;

    public HolyCorpseImplantDoAfterEvent(NetEntity? flashEntity)
    {
        FlashEntity = flashEntity;
    }

    private HolyCorpseImplantDoAfterEvent() { }

    public override DoAfterEvent Clone() => this;
}
