using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.WOU.RemoteHoloParasite;

[RegisterComponent]
public sealed partial class SoulSwappedComponent : Component
{
    [ViewVariables]
    public EntityUid Partner;

    [ViewVariables]
    public EntProtoId ReturnAction = "ActionSoulSwapReturn";

    [ViewVariables]
    public EntityUid? ReturnActionEntity;
}