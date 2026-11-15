using System.Diagnostics.CodeAnalysis;
using Content.Shared._Aquila.Power;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public sealed partial class ChargerSystem
{
    private bool TryGetChargeableBattery(EntityUid uid, [NotNullWhen(true)] out Entity<BatteryComponent>? battery)
    {
        if (_powerCell.TryGetBatteryFromEntityOrSlot(uid, out battery))
            return true;

        var ev = new FindChargerBatteryEvent();
        RaiseLocalEvent(uid, ref ev);
        battery = ev.Battery;
        return battery != null;
    }
}
