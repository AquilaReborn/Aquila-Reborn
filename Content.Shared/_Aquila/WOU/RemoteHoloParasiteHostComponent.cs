using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.WOU.RemoteHoloParasite;

[RegisterComponent]
public sealed partial class RemoteHoloParasiteHostComponent : Component
{
    [DataField]
    public EntProtoId SwapAction = "ActionSoulSwap";

    [ViewVariables]
    public EntityUid? SwapActionEntity;
}