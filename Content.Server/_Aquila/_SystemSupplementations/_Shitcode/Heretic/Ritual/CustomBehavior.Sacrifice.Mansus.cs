using System.Linq;
using Content.Server._Aquila.Heretic;
using Content.Server.Station.Systems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;

namespace Content.Server.Heretic.Ritual;

public partial class RitualSacrificeBehavior
{
    private static void SendSacrificeTargetToMansus(RitualData args, HereticComponent heretic, EntityUid uid)
    {
        if (!heretic.SacrificeTargets.Any(x => x.Entity == args.EntityManager.GetNetEntity(uid)))
            return;

        var station = args.EntityManager.System<StationSystem>().GetOwningStation(args.Platform);
        args.EntityManager.System<MansusRealmSystem>().TrySendToMansus(uid, station);
    }
}
