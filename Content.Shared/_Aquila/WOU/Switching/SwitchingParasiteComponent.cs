using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.WOU.Switching;

[RegisterComponent]
public sealed partial class SwitchingParasiteComponent : Component
{
    /// <summary>
    /// Идентификатор контейнера на носителе, в котором держат человеческую форму.
    /// </summary>
    [DataField] public string HumanContainerId = "switching_parasite_human";

    /// <summary>
    /// Идентификатор контейнера на человеческой форме, в котором держат паразита.
    /// </summary>
    [DataField] public string ParasiteContainerId = "switching_human_parasite";

    /// <summary>
    /// Прототип человеческой формы, в которую переключается паразит.
    /// </summary>
    [DataField] public EntProtoId HumanProto = "MobHumanSatoruAkefu";

    /// <summary>
    /// Прототип действия для переключения на человеческую форму.
    /// </summary>
    [DataField] public EntProtoId SwitchActionProto = "ActionSwitch";

    /// <summary>
    /// Прототип действия для возврата в форму паразита.
    /// </summary>
    [DataField] public EntProtoId ReturnActionProto = "ActionSwitchReturn";

    /// <summary>
    /// Задержка после переключения, в течение которой автоматический возврат не срабатывает.
    /// </summary>
    [DataField] public TimeSpan AutoReturnCooldown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Задержка перед полным лечением обеих форм после переключения.
    /// </summary>
    [DataField] public TimeSpan HealDelay = TimeSpan.FromSeconds(0.05);

    [ViewVariables] public EntityUid? SwitchActionEntity;
    [ViewVariables] public EntityUid? CurrentHuman;
    [ViewVariables] public bool SwitchInitialized;
    [ViewVariables] public TimeSpan NextAutoReturnAllowed = TimeSpan.Zero;
}
