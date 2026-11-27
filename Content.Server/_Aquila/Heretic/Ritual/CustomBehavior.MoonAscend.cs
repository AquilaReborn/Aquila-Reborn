using Content.Server._Aquila.Heretic.Ritual.Helpers;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared.Heretic.Prototypes;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Ритуал вознесения пути Луны: требует трупы с повреждённым более чем наполовину мозгом.
/// </summary>
public sealed partial class RitualMoonAscendBehavior : RitualSacrificeBehavior
{
    private static readonly LocId FailMessage = "heretic-ritual-fail-sacrifice-moon";

    /// <summary>
    /// Доля повреждений мозга, которую необходимо строго превысить для жертвоприношения.
    /// </summary>
    [DataField]
    public float BrainDamageFraction = 0.5f;

    /// <summary>
    /// Отбирает для жертвоприношения только мёртвые тела с достаточно повреждённым мозгом.
    /// </summary>
    public override bool Execute(RitualData args, out string? outstr)
    {
        if (!base.Execute(args, out outstr))
        {
            uids.Clear();
            return false;
        }

        var sanity = args.EntityManager.System<MoonSanitySystem>();
        uids = RitualSacrificeSelectionHelper.SelectCorpses(args.EntityManager, uids, Max, uid =>
        {
            if (!sanity.TryGetBrain(uid, out var brain))
                return false;

            var damage = brain.Comp2.IntegrityCap - brain.Comp2.OrganIntegrity;
            return damage > brain.Comp2.IntegrityCap * BrainDamageFraction;
        });

        if (uids.Count < Min)
        {
            uids.Clear();
            outstr = Loc.GetString(FailMessage);
            return false;
        }

        outstr = null;
        return true;
    }
}
