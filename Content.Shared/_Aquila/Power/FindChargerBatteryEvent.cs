using Content.Shared.Power.Components;

namespace Content.Shared._Aquila.Power;

[ByRefEvent]
public record struct FindChargerBatteryEvent(Entity<BatteryComponent>? Battery = null);
