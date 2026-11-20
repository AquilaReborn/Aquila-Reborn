using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.WOU.Switching;

[RegisterComponent]
public sealed partial class SwitchingComponent : Component
{
    [ViewVariables] public EntityUid Partner;
    [ViewVariables] public EntProtoId ReturnActionProto = "ActionSwitchReturn";
    [ViewVariables] public EntityUid? ReturnActionEntity;
}