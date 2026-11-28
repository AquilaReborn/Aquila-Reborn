using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared.Heretic.Components;
using StatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;

namespace Content.Shared._Shitcode.Heretic.Systems;

public abstract partial class SharedHereticBladeSystem
{
    [Dependency] protected readonly MoonSanitySystem _moonSanity = default!; // Aquila Change
    [Dependency] protected readonly StatusEffectsSystem _moonStatus = default!; // Aquila Change

    /// <summary>
    /// Точка расширения лунного клинка: бьёт по мозгу и накладывает галлюцинации. По безумным и лежачим целям бьёт сильнее.
    /// Реализация живёт на сервере, в классе HereticBladeSystem.
    /// </summary>
    /// <param name="target">Цель удара.</param>
    /// <param name="blade">Лунный клинок.</param>
    protected virtual void ApplyMoonBladeEffect(EntityUid target, HereticBladeComponent? blade) { }
}
