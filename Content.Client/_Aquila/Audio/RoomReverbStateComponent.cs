using Robust.Shared.Audio.Effects;

namespace Content.Client._Aquila.Audio;

/// <summary>
/// Состояние эха в помещении для локального слушателя. Висит на клиентской служебной сущности.
/// </summary>
[RegisterComponent, Access(typeof(RoomReverbSystem))]
public sealed partial class RoomReverbStateComponent : Component
{
    /// <summary>
    /// Сущность эффекта реверберации.
    /// </summary>
    [DataField]
    public EntityUid? Effect;

    /// <summary>
    /// Вспомогательный слот, к которому подключаются звуки.
    /// </summary>
    [DataField]
    public EntityUid? Auxiliary;

    /// <summary>
    /// Когда в следующий раз оценить помещение вокруг слушателя.
    /// </summary>
    [DataField]
    public TimeSpan NextAnalyze;

    /// <summary>
    /// Когда в следующий раз обновить эффект и подключить новые звуки.
    /// </summary>
    [DataField]
    public TimeSpan NextApply;

    [ViewVariables]
    public ReverbProperties Current;

    [ViewVariables]
    public ReverbProperties Target;
}
