using Content.Shared._Aquila.Cyberware.DataDefinitions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.Prototypes;

/// <summary>
/// Стадия киберпсихоза: когда она наступает, какие приступы вызывает и как глючит экран.
/// </summary>
[Prototype]
public sealed partial class CyberpsychosisStagePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Название стадии, которое показывает сканер имплантов.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Риск, с которого стадия может начаться и при котором она держится.
    /// </summary>
    [DataField]
    public float MinRisk;

    /// <summary>
    /// Базовый шанс за одну проверку перейти на эту стадию. Растёт вместе с риском.
    /// </summary>
    [DataField]
    public float Chance = 0.1f;

    /// <summary>
    /// Откатывается ли стадия сама, когда риск падает ниже <see cref="MinRisk"/>.
    /// </summary>
    [DataField]
    public bool Reversible = true;

    /// <summary>
    /// Сообщение, которое видит больной при переходе на эту стадию.
    /// </summary>
    [DataField]
    public LocId? EnterMessage;

    /// <summary>
    /// Сообщение, которое видит больной, когда стадия отступает.
    /// </summary>
    [DataField]
    public LocId? ExitMessage;

    /// <summary>
    /// Наименьшая пауза между приступами.
    /// </summary>
    [DataField]
    public TimeSpan MinEpisodeDelay = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Наибольшая пауза между приступами.
    /// </summary>
    [DataField]
    public TimeSpan MaxEpisodeDelay = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Приступы стадии, из них каждый раз выбирается один.
    /// </summary>
    [DataField]
    public List<CyberpsychosisEpisode> Episodes = new();

    /// <summary>
    /// Компоненты, которые висят на больном, пока длится стадия.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Наименьшая пауза между глитчами на экране.
    /// </summary>
    [DataField]
    public TimeSpan MinGlitchDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Наибольшая пауза между глитчами на экране.
    /// </summary>
    [DataField]
    public TimeSpan MaxGlitchDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Сколько длится один глитч.
    /// </summary>
    [DataField]
    public TimeSpan GlitchDuration = TimeSpan.FromSeconds(0.4);

    /// <summary>
    /// Сила глитча от 0 до 1.
    /// </summary>
    [DataField]
    public float GlitchIntensity = 0.5f;

    /// <summary>
    /// Сила глитча по краям экрана, которая держится постоянно, а не только во время вспышек.
    /// </summary>
    [DataField]
    public float BaseGlitchIntensity;

    /// <summary>
    /// Насколько экран больного заливает красным, от 0 до 1.
    /// </summary>
    [DataField]
    public float Redness;

    /// <summary>
    /// Облики, в которых больному видятся другие люди. Пустой список - видит всех как есть.
    /// </summary>
    [DataField]
    public List<EntProtoId> Disguises = new();

    /// <summary>
    /// Музыка, которую на этой стадии слышит только сам больной.
    /// </summary>
    [DataField]
    public SoundSpecifier? Music;

    /// <summary>
    /// Что видят окружающие при осмотре больного на этой стадии.
    /// </summary>
    [DataField]
    public LocId? ExamineMessage;
}
