using Content.Shared._Aquila.PreCritical;
using Content.Shared.Mobs;
using Robust.Shared.Localization;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerWindow
{
    private static readonly LocId PreCriticalStatus = "health-analyzer-window-entity-pre-critical-text";

    private string GetStatusAquila(EntityUid uid, MobState state)
    {
        return state == MobState.Alive && _entityManager.HasComponent<PreCriticalActiveComponent>(uid)
            ? Loc.GetString(PreCriticalStatus)
            : GetStatus(state);
    }
}
