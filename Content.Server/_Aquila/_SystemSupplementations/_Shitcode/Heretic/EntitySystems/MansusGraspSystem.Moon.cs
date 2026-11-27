using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared.Heretic;
using Content.Shared.StatusEffect;
using StatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class MansusGraspSystem
{
    [Dependency] private readonly MoonSanitySystem _moonSanity = default!;
    [Dependency] private readonly StatusEffectsSystem _moonStatus = default!;

    private static readonly EntProtoId GraspHideStatus = "StatusEffectMoonGraspHide";

    /// <inheritdoc/>
    protected override void ApplyMoonGrasp(EntityUid performer, HereticComponent heretic, EntityUid target)
    {
        if (HasComp<StatusEffectsComponent>(performer))
            _moonStatus.TryAddStatusEffectDuration(performer, GraspHideStatus, heretic.MoonGraspHideDuration);

        if (!HasComp<StatusEffectsComponent>(target))
            return;

        _moonSanity.ApplyBrainDamage(target, heretic.MoonGraspBrainDamage);
        _moonStatus.TryAddStatusEffectDuration(target, MoonSanitySystem.InsanityStatus, heretic.MoonGraspInsanityDuration);
    }
}
