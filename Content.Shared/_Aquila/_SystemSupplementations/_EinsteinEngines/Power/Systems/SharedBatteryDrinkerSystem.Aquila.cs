using System.Diagnostics.CodeAnalysis;
using Content.Shared._Aquila.Power;

namespace Content.Shared._EinsteinEngines.Power.Systems;

public abstract partial class SharedBatteryDrinkerSystem
{
    private bool TryFindDrinkerBattery(EntityUid ent, [NotNullWhen(true)] out EntityUid? drinker)
    {
        var findEv = new FindDrinkerBatteryEvent();
        RaiseLocalEvent(ent, ref findEv);
        drinker = findEv.Battery;
        return drinker != null;
    }
}
