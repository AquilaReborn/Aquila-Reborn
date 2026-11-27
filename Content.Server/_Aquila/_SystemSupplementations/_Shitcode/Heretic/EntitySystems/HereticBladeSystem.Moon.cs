using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared.Heretic.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticBladeSystem
{
    /// <summary>
    /// Серверная реализация лунного клинка: накладывает галлюцинации и бьёт по мозгу,
    /// по безумным и лежачим целям сильнее.
    /// </summary>
    /// <param name="target">Цель удара.</param>
    /// <param name="blade">Лунный клинок.</param>
    protected override void ApplyMoonBladeEffect(EntityUid target, HereticBladeComponent? blade)
    {
        base.ApplyMoonBladeEffect(target, blade);

        if (blade == null || !HasComp<StatusEffectsComponent>(target))
            return;

        _moonStatus.TryAddStatusEffectDuration(target, MoonSanitySystem.InsanityStatus, blade.MoonInsanityDuration);

        var damage = blade.MoonBrainDamage;
        if (TryComp<MobStateComponent>(target, out var mobState) && mobState.CurrentState != MobState.Alive ||
            _moonSanity.IsInsane(target, blade.MoonInsanityThreshold))
            damage = blade.MoonInsaneBrainDamage;

        _moonSanity.ApplyBrainDamage(target, damage);
    }
}
