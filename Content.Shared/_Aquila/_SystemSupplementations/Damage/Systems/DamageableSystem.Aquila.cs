using Content.Shared._Aquila.PreCritical;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    private void RefreshVitalThresholdsAquila(EntityUid uid)
    {
        if (_netMan.IsServer && HasComp<PreCriticalComponent>(uid) && !TerminatingOrDeleted(uid))
            _mobThreshold.VerifyThresholds(uid);
    }
}
