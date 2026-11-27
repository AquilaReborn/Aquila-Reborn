using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticSystem
{
    private void AddKnownRitual(HereticComponent heretic, ProtoId<HereticRitualPrototype> ritual)
    {
        var known = _ritual.GetRitual(ritual);

        if (heretic.KnownRituals.Contains(known))
            return;

        heretic.KnownRituals.Add(known);
    }
}
