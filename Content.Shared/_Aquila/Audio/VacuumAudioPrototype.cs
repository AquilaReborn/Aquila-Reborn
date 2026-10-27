using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Audio;

/// <summary>
/// Настройки звука и речи в вакууме.
/// </summary>
[Prototype]
public sealed partial class VacuumAudioPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Как часто, в секундах, проверяется, находится ли игрок в вакууме.
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
    /// Давление в кПа, ниже которого сущность считается находящейся в вакууме.
    /// </summary>
    [DataField]
    public float VacuumPressureThreshold = 20f;

    /// <summary>
    /// Дальность локальной связи в вакууме, если у обоих собеседников герметичные шлемы.
    /// </summary>
    [DataField]
    public float SuitCommsRange = 7f;
}
