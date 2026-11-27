using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Лунный амулет: при использовании на человеке бьёт по его рассудку, а у достаточно безумной жертвы вызывает буйство.
/// </summary>
[RegisterComponent, Access(typeof(SharedMoonAmuletSystem)), AutoGenerateComponentPause]
public sealed partial class MoonAmuletComponent : Component
{
    /// <summary>
    /// Сколько урона мозгу получает жертва при использовании.
    /// </summary>
    [DataField]
    public FixedPoint2 Damage = 20;

    /// <summary>
    /// Дополнительный урон мозгу, если жертва уже безумна.
    /// </summary>
    [DataField]
    public FixedPoint2 InsaneDamage = 25;

    /// <summary>
    /// Порог урона мозгу, после которого жертва впадает в буйство.
    /// </summary>
    [DataField]
    public float InsaneThreshold = 50f;

    /// <summary>
    /// Длительность галлюцинаций у жертвы.
    /// </summary>
    [DataField]
    public TimeSpan HallucinationDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Длительность оглушения безумной жертвы.
    /// </summary>
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Звук смеха, который слышат окружающие при использовании амулета.
    /// </summary>
    [DataField]
    public SoundSpecifier LaughSound = new SoundCollectionSpecifier("MoonAmuletLaugh");

    /// <summary>
    /// Интервал между применениями амулета.
    /// </summary>
    [DataField]
    public TimeSpan UseDelay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Время, до которого амулет нельзя применить снова.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUse;

    /// <summary>
    /// Носитель амулета, пока он надет. Нужен для снятия эффектов при удалении амулета.
    /// </summary>
    [DataField]
    public EntityUid? Wearer;

    /// <summary>
    /// Прототип визуального эффекта на жертве.
    /// </summary>
    [DataField]
    public EntProtoId Effect = "EffectMoonAmuletPulse";

    /// <summary>
    /// Во сколько раз ускоряется восстановление мозга носителя, пока амулет надет.
    /// </summary>
    [DataField]
    public float BrainHealMultiplier = 2f;

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
}
