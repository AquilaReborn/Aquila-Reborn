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

[Serializable, NetSerializable]
public sealed partial class SpinMarkingDoAfterEvent : SimpleDoAfterEvent { }

public sealed partial class SpinStunBlowEvent : InstantActionEvent { }
public sealed partial class SpinMarkingEvent : InstantActionEvent { }

public sealed class SpinStunHitEvent : EntityEventArgs
{
    public EntityUid Target;
    public SpinStunHitEvent(EntityUid target) => Target = target;
}

public sealed class SpinMarkHitEvent : EntityEventArgs
{
    public EntityUid Target;
    public EntityUid Performer;
    public SpinMarkHitEvent(EntityUid target, EntityUid performer)
    {
        Target = target;
        Performer = performer;
    }
}
