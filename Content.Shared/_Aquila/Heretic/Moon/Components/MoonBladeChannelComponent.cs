using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Состояние лунного клинка, который проводят через надетый лунный амулет: физический урон убран, вместо него бьёт по мозгу.
/// </summary>
[RegisterComponent, Access(typeof(SharedHereticBladeSystem))]
public sealed partial class MoonBladeChannelComponent : Component
{
    /// <summary>
    /// Исходный физический урон клинка, который возвращается после снятия амулета.
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage;

    /// <summary>
    /// Во сколько раз урон мозгу, проводящийся через клинок, превышает исходный урон клинка.
    /// </summary>
    [DataField]
    public float BrainMultiplier = 1f;

    /// <summary>
    /// Длительность галлюцинаций, которые клинок накладывает через амулет.
    /// </summary>
    [DataField]
    public TimeSpan InsanityDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Прототип визуального эффекта, который клинок оставляет на жертве.
    /// </summary>
    [DataField]
    public EntProtoId? Effect;
}
