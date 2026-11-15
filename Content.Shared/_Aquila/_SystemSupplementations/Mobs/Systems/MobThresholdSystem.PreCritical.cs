using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.PreCritical;
using Content.Shared.Alert;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mobs.Systems;

public sealed partial class MobThresholdSystem
{
    [Dependency] private readonly PreCriticalSystem _preCritical = default!;

    private static readonly ProtoId<AlertPrototype> PreCriticalAlert = "HumanPreCritical";

    private bool UpdatePreCriticalAquila(EntityUid uid, MobState state, MobThresholdsComponent thresholds, DamageableComponent damageable, bool refresh)
    {
        if (!TryComp<PreCriticalComponent>(uid, out var settings))
            return false;

        if (!refresh || _net.IsClient)
            return HasComp<PreCriticalActiveComponent>(uid);

        TryGetIncapThreshold(uid, out var incapThreshold, thresholds);
        return _preCritical.UpdateEffect((uid, settings), state, CheckVitalDamage(uid, damageable), incapThreshold);
    }

    private static ProtoId<AlertPrototype> SelectHealthAlertAquila(bool preCritical, ProtoId<AlertPrototype> currentAlert)
    {
        return preCritical ? PreCriticalAlert : currentAlert;
    }

    private void ClearPreCriticalAquila(EntityUid uid)
    {
        _preCritical.ClearEffect(uid, _mobStateSystem.IsAlive(uid));
    }

    private bool TryGetAlertPercentageAquila(EntityUid uid, MobState state, MobState nextState, DamageableComponent damageable,
        [NotNullWhen(true)] out FixedPoint2? percentage)
    {
        var damage = CheckVitalDamage(uid, damageable);
        if (state == MobState.Alive && TryComp<PreCriticalComponent>(uid, out var settings) &&
            settings.DamageThreshold > FixedPoint2.Zero &&
            TryGetThresholdForState(uid, nextState, out var nextThreshold) && settings.DamageThreshold < nextThreshold.Value)
        {
            percentage = damage / settings.DamageThreshold;
            return true;
        }

        return TryGetPercentageForState(uid, nextState, damage, out percentage);
    }
}
