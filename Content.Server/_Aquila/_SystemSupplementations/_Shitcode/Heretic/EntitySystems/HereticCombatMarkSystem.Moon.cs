using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared.Heretic;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticCombatMarkSystem
{
    [Dependency] private readonly MoonSanitySystem _moonSanity = default!;
    [Dependency] private readonly StatusEffectsSystem _moonStatus = default!;

    /// <summary>
    /// Детонация метки Луны: урон мозгу и помутнение рассудка.
    /// </summary>
    /// <param name="target">Цель, на которой сработала метка.</param>
    /// <param name="mark">Сработавшая метка.</param>
    private void ApplyMoonMarkEffect(EntityUid target, HereticCombatMarkComponent mark)
    {
        _moonSanity.ApplyBrainDamage(target, mark.MoonBrainDamage);
        _moonStatus.TryAddStatusEffectDuration(target, MoonSanitySystem.InsanityStatus, mark.MoonInsanityDuration);
    }
}
