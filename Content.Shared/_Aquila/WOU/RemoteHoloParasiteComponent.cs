using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.WOU.RemoteHoloParasite;

[RegisterComponent]
public sealed partial class RemoteHoloParasiteComponent : Component
{
    [DataField]
    public EntProtoId SwapAction = "ActionSoulSwap";

    [DataField]
    public EntProtoId ReturnAction = "ActionSoulSwapReturn";

    [ViewVariables]
    public EntityUid? LinkedHost;

    [ViewVariables]
    public bool HostLinked;
}