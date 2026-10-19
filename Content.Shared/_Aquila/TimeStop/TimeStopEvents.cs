using Content.Shared.Actions;

namespace Content.Shared.Aquila.TimeStop;

public sealed partial class TimeStopActionEvent : InstantActionEvent;

[ByRefEvent]
public record struct RemoveTimeStopSlowdownEvent(EntityUid Source);
