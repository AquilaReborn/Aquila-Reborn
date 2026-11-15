using Content.Shared.Actions;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.WOU.Switching;

[RegisterComponent]
public sealed partial class SwitchingParasiteComponent : Component
{
    [DataField] public string HumanContainerId = "switching_parasite_human";
    [DataField] public string ParasiteContainerId = "switching_human_parasite";
    [DataField] public EntProtoId HumanProto = "MobHumanSatoruAkefu";
    [DataField] public EntProtoId SwitchActionProto = "ActionSwitch";
    [DataField] public EntProtoId ReturnActionProto = "ActionSwitchReturn";
    
    [ViewVariables] public EntityUid? SwitchActionEntity;
    [ViewVariables] public EntityUid? CurrentHuman;
    [ViewVariables] public bool Initialized;
    
    // Время, до которого автоматический возврат заблокирован (защита после Switch)
    [ViewVariables] public TimeSpan NextAutoReturnAllowed = TimeSpan.Zero; 
}

[RegisterComponent]
public sealed partial class SwitchingComponent : Component
{
    [ViewVariables] public EntityUid Partner;
    [ViewVariables] public EntProtoId ReturnActionProto = "ActionSwitchReturn";
    [ViewVariables] public EntityUid? ReturnActionEntity;
}

[RegisterComponent]
public sealed partial class SwitchingHumanProfileComponent : Component
{
    [DataField] public HumanoidCharacterProfile Profile = new();
}

public sealed partial class SwitchActionEvent : InstantActionEvent;
public sealed partial class SwitchReturnActionEvent : InstantActionEvent;