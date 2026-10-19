using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.HolyCorpse;

[Serializable, NetSerializable]
public sealed partial class HolyCorpseCreatorDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetEntity? FlashEntity;

    public HolyCorpseCreatorDoAfterEvent(NetEntity? flashEntity)
    {
        FlashEntity = flashEntity;
    }

    private HolyCorpseCreatorDoAfterEvent() { }

    public override DoAfterEvent Clone() => this;
}
