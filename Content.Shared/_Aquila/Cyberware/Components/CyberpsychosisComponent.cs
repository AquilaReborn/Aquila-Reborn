using Content.Shared._Aquila.Cyberware.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.Components;

/// <summary>
/// Киберпсихоз: организм не справляется с количеством железа в нём.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedCyberpsychosisSystem))]
public sealed partial class CyberpsychosisComponent : Component
{
    public override bool SendOnlyToOwner => true;

    /// <summary>
    /// Текущая стадия.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<CyberpsychosisStagePrototype> Stage;

    /// <summary>
    /// Когда случится следующий приступ.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextEpisode;

    /// <summary>
    /// Когда больной услышит следующую звуковую галлюцинацию.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextHallucination;

    /// <summary>
    /// Заглушены ли приступы лекарством.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Suppressed;

    /// <summary>
    /// До какого момента приступы заглушены.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan SuppressedUntil;

    /// <summary>
    /// Сколько секунд курса лечения нужно пройти без перерыва, чтобы вылечиться.
    /// </summary>
    [DataField]
    public float CureTime = 120f;

    /// <summary>
    /// Сколько секунд курса уже пройдено.
    /// </summary>
    [ViewVariables]
    public float CureProgress;

    /// <summary>
    /// Сколько секунд курса теряется за секунду, если лекарства в крови нет.
    /// </summary>
    [DataField]
    public float CureDecay = 0.5f;

    /// <summary>
    /// Сколько можно обходиться без лекарства, прежде чем курс начнёт откатываться.
    /// </summary>
    [DataField]
    public TimeSpan TherapyGrace = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Когда лекарство последний раз действовало.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan LastTherapy;

    /// <summary>
    /// Когда можно снова предупредить, что лекарство не справляется с нагрузкой.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextTherapyWarning;

    /// <summary>
    /// Как часто напоминать, что лекарство не справляется.
    /// </summary>
    [DataField]
    public TimeSpan TherapyWarningDelay = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Компоненты, которые добавила текущая стадия и которые нужно снять при её смене.
    /// </summary>
    [ViewVariables]
    public List<string> AddedComponents = new();

    /// <summary>
    /// Когда на экране начнётся следующий глитч. Считается только на клиенте.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextGlitch;

    /// <summary>
    /// Когда закончится текущий глитч. Считается только на клиенте.
    /// </summary>
    [ViewVariables]
    public TimeSpan GlitchEnd;

    /// <summary>
    /// Когда в следующий раз обновлять враждебные облики окружающих. Считается только на клиенте.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextDisguiseUpdate;

    /// <summary>
    /// Музыка стадии, которая сейчас играет у больного. Существует только на клиенте.
    /// </summary>
    [ViewVariables]
    public EntityUid? MusicStream;
}
