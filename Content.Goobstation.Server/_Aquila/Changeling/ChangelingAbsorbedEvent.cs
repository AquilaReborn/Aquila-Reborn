namespace Content.Goobstation.Server._Aquila.Changeling;

/// <summary>
/// Вызывается на генокраде после того, как он поглотил цель.
/// </summary>
[ByRefEvent]
public readonly record struct ChangelingAbsorbedEvent(EntityUid Target);
