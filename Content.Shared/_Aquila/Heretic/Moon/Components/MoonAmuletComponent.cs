using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Лунный амулет обращает обезумевших членов экипажа в союзников еретика.
/// </summary>
[RegisterComponent, Access(typeof(SharedMoonAmuletSystem)), AutoGenerateComponentPause]
public sealed partial class MoonAmuletComponent : Component
{
    private static readonly EntProtoId DefaultLunaticAmulet = "ClothingNeckMoonAmuletBound";

    /// <summary>
    /// Путь еретика, которому доступно обращение через этот амулет.
    /// </summary>
    [DataField]
    public string RequiredPath = "Moon";

    /// <summary>
    /// Порог безумия для обращения в лунатика.
    /// </summary>
    [DataField]
    public float ConversionThreshold = 20f;

    /// <summary>
    /// Амулет, который получает обращённый лунатик.
    /// </summary>
    [DataField]
    public EntProtoId LunaticAmulet = DefaultLunaticAmulet;

    /// <summary>
    /// Сколько урона мозгу получает жертва при использовании.
    /// Устаревшее поле, сохранено только для загрузки старых карт.
    /// </summary>
    [DataField]
    public FixedPoint2 Damage = 20;

    /// <summary>
    /// Дополнительный урон мозгу, если жертва уже безумна.
    /// Устаревшее поле, сохранено только для загрузки старых карт.
    /// </summary>
    [DataField]
    public FixedPoint2 InsaneDamage = 25;

    /// <summary>
    /// Порог урона мозгу, после которого жертва впадает в буйство.
    /// Устаревшее поле, сохранено только для загрузки старых карт.
    /// </summary>
    [DataField]
    public float InsaneThreshold = 50f;

    /// <summary>
    /// Длительность галлюцинаций у жертвы.
    /// Устаревшее поле, сохранено только для загрузки старых карт.
    /// </summary>
    [DataField]
    public TimeSpan HallucinationDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Длительность оглушения безумной жертвы.
    /// Устаревшее поле, сохранено только для загрузки старых карт.
    /// </summary>
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Звук приветствия, который слышит только обращённый лунатик.
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
    /// Устаревшее поле, сохранено только для загрузки старых карт.
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
    /// Устаревшее поле, сохранено только для загрузки старых карт.
    /// </summary>
    [DataField]
    public float BrainHealMultiplier = 2f;

    /// <summary>
    /// Во сколько раз урон мозгу, проводящийся через лунный клинок, превышает урон клинка.
    /// Устаревшее поле, сохранено только для загрузки старых карт.
    /// </summary>
    [DataField]
    public float ChannelBrainMultiplier = 1f;

    /// <summary>
    /// Длительность галлюцинаций, которые клинок накладывает через амулет.
    /// Устаревшее поле, сохранено только для загрузки старых карт.
    /// </summary>
    [DataField]
    public TimeSpan ChannelInsanityDuration = TimeSpan.FromSeconds(5);
}
