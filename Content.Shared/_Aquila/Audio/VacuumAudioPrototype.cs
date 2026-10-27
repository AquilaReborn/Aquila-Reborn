using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Audio;

/// <summary>
/// Настройки приглушения звуков в вакууме.
/// </summary>
[Prototype]
public sealed partial class VacuumAudioPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Как часто, в секундах, проверяется, находится ли слушатель в вакууме.
    /// </summary>
    [DataField]
    public float UpdateInterval = 0.25f;

    /// <summary>
    /// Дополнительная окклюзия всех звуков, когда слушатель в вакууме.
    /// </summary>
    [DataField]
    public float ListenerOcclusion = 3f;

    /// <summary>
    /// Дополнительная окклюзия звука, источник которого находится в открытом космосе.
    /// </summary>
    [DataField]
    public float SourceOcclusion = 2f;

    /// <summary>
    /// Значок, по которому определяется, что слушатель в вакууме.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> PressureAlert = "LowPressure";

    /// <summary>
    /// С какой степени значка давления слушатель считается находящимся в вакууме.
    /// </summary>
    [DataField]
    public short VacuumSeverity = 2;

    /// <summary>
    /// Давление в кПа, ниже которого речь слышна только по рации, а у эмоций пропадает звук.
    /// </summary>
    [DataField]
    public float SpeechPressureThreshold = 20f;
}
