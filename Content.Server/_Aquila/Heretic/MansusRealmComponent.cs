using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic;

[RegisterComponent, Access(typeof(MansusRealmSystem))]
public sealed partial class MansusRealmComponent : Component
{
    /// <summary>
    /// Размер стороны квадратной платформы мира испытания в тайлах.
    /// </summary>
    [DataField]
    public int PlatformSize = 10;

    /// <summary>
    /// Тайл, которым выкладывается платформа испытания.
    /// </summary>
    [DataField]
    public ProtoId<ContentTileDefinition> PlatformTile = "FloorDarkMarble";

    /// <summary>
    /// Параллакс карты мира испытания.
    /// </summary>
    [DataField]
    public string Parallax = "AquilaMansus";

    /// <summary>
    /// Цвет окружающего света карты мира испытания.
    /// </summary>
    [DataField]
    public Color AmbientLight = Color.FromHex("#9b86c4");

    /// <summary>
    /// Слоты инвентаря, предметы из которых копируются жертве в мир испытания.
    /// </summary>
    [DataField]
    public List<string> CarriedSlots = new() { "jumpsuit", "shoes" };

    /// <summary>
    /// Прототипы рук, из которых случайно выбирается рука для спавна.
    /// </summary>
    [DataField]
    public List<EntProtoId> HandPrototypes = new() { "MansusHand", "MansusHandRight" };

    /// <summary>
    /// Радиус, на котором руки появляются вокруг центра платформы.
    /// </summary>
    [DataField]
    public float SpawnRadius = 12f;

    /// <summary>
    /// Минимальная скорость полёта рук.
    /// </summary>
    [DataField]
    public float MinHandSpeed = 3f;

    /// <summary>
    /// Максимальная скорость полёта рук.
    /// </summary>
    [DataField]
    public float MaxHandSpeed = 4.5f;

    /// <summary>
    /// Пауза после переноса жертвы, в течение которой руки не появляются.
    /// </summary>
    [DataField]
    public TimeSpan GracePeriod = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Сколько жертва должна продержаться, чтобы пройти испытание.
    /// </summary>
    [DataField]
    public TimeSpan SurviveDuration = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Начальный интервал между одиночными руками.
    /// </summary>
    [DataField]
    public float StartInterval = 1.5f;

    /// <summary>
    /// Конечный интервал между одиночными руками, плавно достигается к концу испытания.
    /// </summary>
    [DataField]
    public float EndInterval = 0.55f;

    /// <summary>
    /// Множитель паузы перед следующим тяжёлым паттерном.
    /// </summary>
    [DataField]
    public float HeavyIntervalMultiplier = 2.5f;

    /// <summary>
    /// Через сколько секунд активной фазы открываются тяжёлые паттерны.
    /// </summary>
    [DataField]
    public float PatternUnlockTime = 8f;

    /// <summary>
    /// Через сколько секунд активной фазы открывается паттерн стены.
    /// </summary>
    [DataField]
    public float WallUnlockTime = 20f;

    /// <summary>
    /// Вес веера при выборе тяжёлого паттерна.
    /// </summary>
    [DataField]
    public float FanWeight = 0.6f;

    /// <summary>
    /// Вес клешни при выборе тяжёлого паттерна.
    /// </summary>
    [DataField]
    public float PincerWeight = 0.6f;

    /// <summary>
    /// Вес стены при выборе тяжёлого паттерна.
    /// </summary>
    [DataField]
    public float WallWeight = 0.4f;

    /// <summary>
    /// Множитель паузы для веера при расчёте следующего тяжёлого паттерна.
    /// </summary>
    [DataField]
    public float FanCost = 2.2f;

    /// <summary>
    /// Множитель паузы для клешни при расчёте следующего тяжёлого паттерна.
    /// </summary>
    [DataField]
    public float PincerCost = 1.8f;

    /// <summary>
    /// Множитель паузы для стены при расчёте следующего тяжёлого паттерна.
    /// </summary>
    [DataField]
    public float WallCost = 3.5f;

    /// <summary>
    /// Угол разброса между руками веера в градусах.
    /// </summary>
    [DataField]
    public float FanAngle = 14f;

    /// <summary>
    /// Расстояние между руками в паттерне стены.
    /// </summary>
    [DataField]
    public float WallSpacing = 0.8f;

    /// <summary>
    /// Ширина безопасного прохода в паттерне стены в руках.
    /// </summary>
    [DataField]
    public int WallGap = 3;

    /// <summary>
    /// Добавка к времени полёта рук при расчёте блокировки следующего паттерна.
    /// </summary>
    [DataField]
    public float LockoutPadding = 0.3f;

    /// <summary>
    /// Количество рун по периметру платформы.
    /// </summary>
    [DataField]
    public int RuneCount = 12;

    /// <summary>
    /// Прототип потухшей руны.
    /// </summary>
    [DataField]
    public EntProtoId DormantRune = "MansusRuneDormant";

    /// <summary>
    /// Прототип загоревшейся руны.
    /// </summary>
    [DataField]
    public EntProtoId LitRune = "MansusRuneLit";

    /// <summary>
    /// Музыка, которая играет жертве во время испытания.
    /// </summary>
    [DataField]
    public SoundSpecifier Music = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/VoidsEmbrace.ogg", AudioParams.Default.WithLoop(true));

    /// <summary>
    /// Громкость, до которой плавно затухает музыка при уходе жертвы.
    /// </summary>
    [DataField]
    public float MusicFadeVolume = -30f;

    /// <summary>
    /// Длительность плавного ухода жертвы с затуханием музыки.
    /// </summary>
    [DataField]
    public TimeSpan LeaveDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Длительность сна, который получает жертва после успешного возврата.
    /// </summary>
    [DataField]
    public TimeSpan SleepDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Прототип эффекта, который создаётся на месте возврата жертвы.
    /// </summary>
    [DataField]
    public EntProtoId ReturnEffect = "EffectVoidBlinkIn";

    /// <summary>
    /// Звук, который проигрывается при возврате жертвы.
    /// </summary>
    [DataField]
    public SoundSpecifier ReturnSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/voidblink.ogg");

    /// <summary>
    /// Копия тела жертвы в мире испытания.
    /// </summary>
    [ViewVariables]
    public EntityUid Victim;

    /// <summary>
    /// Разум жертвы, перенесённый в копию тела.
    /// </summary>
    [ViewVariables]
    public EntityUid Mind;

    /// <summary>
    /// Грид с платформой испытания.
    /// </summary>
    [ViewVariables]
    public EntityUid Grid;

    /// <summary>
    /// Станция, на которую вернётся жертва.
    /// </summary>
    [ViewVariables]
    public EntityUid? ReturnStation;

    /// <summary>
    /// Сущность звукового потока музыки испытания.
    /// </summary>
    [ViewVariables]
    public EntityUid? MusicStream;

    /// <summary>
    /// Время начала испытания.
    /// </summary>
    [ViewVariables]
    public TimeSpan StartTime;

    /// <summary>
    /// Время следующего появления одиночной руки.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextHand;

    /// <summary>
    /// Время следующего появления тяжёлого паттерна.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextHeavy;

    /// <summary>
    /// Время начала ухода жертвы, null пока уход не начался.
    /// </summary>
    [ViewVariables]
    public TimeSpan? LeaveTime;

    /// <summary>
    /// Все руны на платформе в порядке загорания.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Runes = new();

    /// <summary>
    /// Сколько рун уже зажглось.
    /// </summary>
    [ViewVariables]
    public int LitRunes;

    /// <summary>
    /// Флаг, что испытание завершено и мир можно удалять.
    /// </summary>
    [ViewVariables]
    public bool Finished;

    /// <summary>
    /// Еретик, которому уйдёт отчёт об исходе испытания, может быть null.
    /// </summary>
    [ViewVariables]
    public EntityUid? Heretic;
}
