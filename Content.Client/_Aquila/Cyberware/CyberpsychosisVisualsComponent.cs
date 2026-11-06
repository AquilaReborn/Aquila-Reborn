namespace Content.Client._Aquila.Cyberware;

/// <summary>
/// Что больной киберпсихозом видит и слышит у себя на клиенте: таймеры глитчей и обликов и музыка стадии.
/// </summary>
[RegisterComponent, Access(typeof(CyberpsychosisSystem))]
public sealed partial class CyberpsychosisVisualsComponent : Component
{
    /// <summary>
    /// Когда на экране начнётся следующий глитч.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextGlitch;

    /// <summary>
    /// Когда закончится текущий глитч.
    /// </summary>
    [ViewVariables]
    public TimeSpan GlitchEnd;

    /// <summary>
    /// Когда в следующий раз обновлять враждебные облики окружающих.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextDisguiseUpdate;

    /// <summary>
    /// Музыка стадии, которая сейчас играет у больного.
    /// </summary>
    [ViewVariables]
    public EntityUid? MusicStream;
}
