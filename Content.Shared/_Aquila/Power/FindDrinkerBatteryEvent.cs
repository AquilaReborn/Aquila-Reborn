namespace Content.Shared._Aquila.Power;

[ByRefEvent]
public record struct FindDrinkerBatteryEvent(EntityUid? Battery = null);
