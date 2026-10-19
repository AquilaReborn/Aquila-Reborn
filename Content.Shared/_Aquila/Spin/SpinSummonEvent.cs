using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.Spin;

public sealed partial class SpinSummonEvent : InstantActionEvent
{
}

public sealed partial class SpinEmpowerEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class SpinEmpowerDoAfterEvent : DoAfterEvent
{
    [DataField]
    public int Step;

    private SpinEmpowerDoAfterEvent()
    {
    }

    public SpinEmpowerDoAfterEvent(int step)
    {
        Step = step;
    }

    public override DoAfterEvent Clone() => this;
}
