using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.RoundEnd;

public sealed partial class RoundEndSummaryWindow
{
    private static readonly (decimal Threshold, LocId Loc)[] DeathSeverities =
    {
        (1000, "round-end-summary-window-death-severity-catastrophic"),
        (750, "round-end-summary-window-death-severity-devastating"),
        (500, "round-end-summary-window-death-severity-agonizing"),
        (300, "round-end-summary-window-death-severity-painful"),
        (200, "round-end-summary-window-death-severity-brutal"),
    };

    private static readonly LocId DeathSeverityDefault = "round-end-summary-window-death-severity-tragic";

    private static readonly Dictionary<ProtoId<DamageGroupPrototype>, LocId> DeathTypes = new()
    {
        ["Burn"] = "round-end-summary-window-death-type-fiery",
        ["Brute"] = "round-end-summary-window-death-type-crushing",
        ["Toxin"] = "round-end-summary-window-death-type-poisonous",
        ["Airloss"] = "round-end-summary-window-death-type-suffocating",
        ["Genetic"] = "round-end-summary-window-death-type-twisted",
        ["Metaphysical"] = "round-end-summary-window-death-type-otherworldly",
        ["Electronic"] = "round-end-summary-window-death-type-shocking",
    };

    private static readonly LocId DeathTypeDefault = "round-end-summary-window-death-type-mysterious";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static string GetDeathSeverity(decimal totalDamage)
    {
        foreach (var (threshold, loc) in DeathSeverities)
        {
            if (totalDamage >= threshold)
                return Loc.GetString(loc);
        }

        return Loc.GetString(DeathSeverityDefault);
    }

    private static string GetDeathType(string damageGroup)
    {
        return Loc.GetString(DeathTypes.GetValueOrDefault(damageGroup, DeathTypeDefault));
    }

    private string GetDamageGroupName(string damageGroup)
    {
        return _prototypeManager.TryIndex<DamageGroupPrototype>(damageGroup, out var group)
            ? group.LocalizedName
            : damageGroup;
    }
}
