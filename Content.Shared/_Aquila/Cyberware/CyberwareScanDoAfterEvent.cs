using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Cyberware;

[Serializable, NetSerializable]
public sealed partial class CyberwareScanDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// Глубокое ли это сканирование, которое раскрывает все импланты.
    /// </summary>
    [DataField]
    public bool Deep;

    public CyberwareScanDoAfterEvent(bool deep)
    {
        Deep = deep;
    }

    private CyberwareScanDoAfterEvent() { }

    public override DoAfterEvent Clone() => this;
}
