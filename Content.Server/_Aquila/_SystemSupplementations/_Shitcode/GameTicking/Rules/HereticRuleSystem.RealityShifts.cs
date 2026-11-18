using Content.Server.GameTicking.Rules.Components;
using Content.Server.Heretic.EntitySystems;
using Content.Shared.Station.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class HereticRuleSystem
{
    [Dependency] private readonly HereticSystem _aquilaHeretic = default!;

    private void AquilaOnHereticMade(EntityUid target, HereticRuleComponent rule)
    {
        _aquilaHeretic.RefreshVisibility(target);
        AquilaSpawnRealityShifts(rule);
    }

    private void AquilaSpawnRealityShifts(HereticRuleComponent rule)
    {
        if (!TryGetRandomStation(out var station))
            return;

        var grid = GetStationMainGrid(Comp<StationDataComponent>(station.Value));

        if (grid == null)
            return;

        for (var i = 0; i < rule.RealityShiftPerHeretic.Next(_rand); i++)
        {
            if (TryFindTileOnGrid(grid.Value, out _, out var coords))
                SpawnAttachedTo(rule.RealityShift, coords);
        }
    }
}
