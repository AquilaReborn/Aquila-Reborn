using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Аура вознёсшегося еретика пути Луны: постоянно подтачивает рассудок окружающих.
/// </summary>
[RegisterComponent, Access(typeof(SharedMoonAscensionSystem)), AutoGenerateComponentPause]
public sealed partial class MoonAscensionComponent : Component
{
    /// <summary>
    /// Как часто аура срабатывает.
    /// </summary>
    [DataField]
    public TimeSpan PulseInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Радиус ауры в тайлах.
    /// </summary>
    [DataField]
    public float Radius = 7f;

    /// <summary>
    /// Сколько урона мозгу получает каждая жертва за срабатывание.
    /// </summary>
    [DataField]
    public FixedPoint2 BrainDamage = 5;

    /// <summary>
    /// Длительность галлюцинаций, накладываемых аурой. Должна быть больше интервала пульса, чтобы оверлей не мигал.
    /// </summary>
    [DataField]
    public TimeSpan HallucinationDuration = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Прототип разового визуального эффекта, который появляется на жертве.
    /// </summary>
    [DataField]
    public EntProtoId Effect = "EffectMoonInsanity";

    /// <summary>
    /// Прототип постоянного оверлея безумия, который висит на жертве, пока её держит аура.
    /// </summary>
    [DataField]
    public EntProtoId InsanityOverlay = "EffectMoonAscensionInsanity";

    /// <summary>
    /// Прототип визуальной ауры вокруг самого вознёсшегося еретика.
    /// </summary>
    [DataField]
    public EntProtoId AuraVisual = "EffectMoonAscensionAura";

    /// <summary>
    /// Сущность визуальной ауры вокруг еретика.
    /// </summary>
    public EntityUid? AuraVisualEntity;

    /// <summary>
    /// Сколько экипажа обращается в лунатиков за одно срабатывание.
    /// </summary>
    [DataField]
    public int LunaticsPerConversion = 1;

    /// <summary>
    /// Порог урона мозгу от Луны, после которого жертва достаточно безумна для обращения.
    /// </summary>
    [DataField]
    public float LunaticSanityThreshold = 20f;

    /// <summary>
    /// Какая доля экипажа обращается в лунатиков при вознесении, помимо уже обезумевших.
    /// </summary>
    [DataField]
    public float AscensionConvertedFraction = 0.2f;

    /// <summary>
    /// Как часто аура обращает экипаж в лунатиков.
    /// </summary>
    [DataField]
    public TimeSpan ConversionInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Прототип лунного амулета, который получает обращённый.
    /// </summary>
    [DataField]
    public EntProtoId LunaticAmulet = "ClothingNeckMoonAmuletBound";

    /// <summary>
    /// Звук, который слышит обращённый.
    /// </summary>
    [DataField]
    public SoundSpecifier? LunaticSound = new SoundCollectionSpecifier("MoonAmuletLaugh");

    /// <summary>
    /// Минимальный интервал в секундах между приступами смеха амулета у лунатика.
    /// </summary>
    [DataField]
    public float LunaticLaughterMinInterval = 4f;

    /// <summary>
    /// Максимальный интервал в секундах между приступами смеха амулета у лунатика.
    /// </summary>
    [DataField]
    public float LunaticLaughterMaxInterval = 9f;

    /// <summary>
    /// Насколько далеко от лунатика может звучать смех амулета.
    /// </summary>
    [DataField]
    public float LunaticLaughterDistance = 7f;

    /// <summary>
    /// Время следующего срабатывания ауры.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;

    /// <summary>
    /// Время следующего обращения экипажа в лунатиков.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextConversion;
}
