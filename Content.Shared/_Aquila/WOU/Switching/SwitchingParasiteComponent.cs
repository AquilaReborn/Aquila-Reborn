using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.WOU.Switching;

[RegisterComponent]
public sealed partial class SwitchingParasiteComponent : Component
{
    [DataField] public string HumanContainerId = "switching_parasite_human";
    [DataField] public string ParasiteContainerId = "switching_human_parasite";
    [DataField] public EntProtoId HumanProto = "MobHumanSatoruAkefu";
    [DataField] public EntProtoId SwitchActionProto = "ActionSwitch";
    [DataField] public EntProtoId ReturnActionProto = "ActionSwitchReturn";
    [DataField] public TimeSpan AutoReturnCooldown = TimeSpan.FromSeconds(2);
    [DataField] public TimeSpan HealDelay = TimeSpan.FromSeconds(0.05);

    [ViewVariables] public EntityUid? SwitchActionEntity;
    [ViewVariables] public EntityUid? CurrentHuman;
    [ViewVariables] public bool Initialized;
    [ViewVariables] public TimeSpan NextAutoReturnAllowed = TimeSpan.Zero;
}