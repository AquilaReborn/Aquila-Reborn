using Content.Shared._Aquila.Heretic.Moon.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Носитель лунного амулета из числа еретиков, гулей или лунатиков: ускоряет восстановление мозга и проводит урон через лунный клинок.
/// </summary>
[RegisterComponent, Access(typeof(SharedMoonAmuletSystem))]
public sealed partial class MoonAmuletWearerComponent : Component
{
    /// <summary>
    /// Во сколько раз ускоряется восстановление мозга владельца.
    /// </summary>
    [DataField]
    public float BrainHealMultiplier = 1f;

    /// <summary>
    /// Во сколько раз урон мозгу, проводящийся через лунный клинок, превышает урон клинка.
    /// </summary>
    [DataField]
    public float ChannelBrainMultiplier = 1f;

    /// <summary>
    /// Длительность галлюцинаций, которые клинок накладывает через амулет.
    /// </summary>
    [DataField]
    public TimeSpan ChannelInsanityDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Прототип визуального эффекта, который клинок оставляет на жертве.
    /// </summary>
    [DataField]
    public EntProtoId? ChannelEffect;
}
