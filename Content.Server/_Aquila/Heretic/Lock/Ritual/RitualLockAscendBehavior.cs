using System.Linq;
using Content.Server._Aquila.Heretic.Ritual.Helpers;
using Content.Server.Heretic.Ritual;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic.Lock.Ritual;

/// <summary>
/// Требует нужное число трупов с пустой грудью и открывает разрыв в реальности на месте ритуала.
/// </summary>
public sealed partial class RitualLockAscendBehavior : RitualSacrificeBehavior
{
    private static readonly EntProtoId LockRift = "HereticLockRift";

    private static readonly LocId SacrificeFailMessage = "heretic-ritual-fail-sacrifice-lock";

    /// <summary>
    /// Отбирает для жертвоприношения мёртвые тела, у которых сохранилась грудь без органов.
    /// </summary>
    public override bool Execute(RitualData args, out string? outstr)
    {
        if (!base.Execute(args, out outstr))
        {
            uids.Clear();
            return false;
        }

        var body = args.EntityManager.System<SharedBodySystem>();

        uids = RitualSacrificeSelectionHelper.SelectCorpses(args.EntityManager, uids, Max, uid =>
        {
            var hasChest = false;
            var hasOrgans = false;
            foreach (var (partId, _) in body.GetBodyChildrenOfType(uid, BodyPartType.Chest))
            {
                hasChest = true;
                if (!body.GetPartOrgans(partId).Any())
                    continue;

                hasOrgans = true;
                break;
            }

            return hasChest && !hasOrgans;
        });

        if (uids.Count < Min)
        {
            uids.Clear();
            outstr = Loc.GetString(SacrificeFailMessage);
            return false;
        }

        outstr = null;
        return true;
    }

    /// <summary>
    /// Приносит отобранные тела в жертву и однократно открывает разрыв после завершения жертвоприношения.
    /// </summary>
    public override void Finalize(RitualData args)
    {
        if (uids.Count < Min)
            return;

        base.Finalize(args);

        if (uids.Count != 0 || !args.EntityManager.TryGetComponent<TransformComponent>(args.Platform, out var xform))
            return;

        var coords = xform.Coordinates;
        args.EntityManager.SpawnAtPosition(LockRift, coords);
    }
}
