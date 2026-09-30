using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.HideBodyLayers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(ToggleHideLayersClothingSystem))]
public sealed partial class ToggleHideLayersClothingComponent : Component
{
    [DataField(required: true)]
    public HashSet<HumanoidVisualLayers> Layers = new();

    [DataField]
    public SlotFlags Slots = SlotFlags.OUTERCLOTHING | SlotFlags.HEAD;

    [DataField]
    public EntProtoId Action = "ActionToggleHideLayersSuit";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public bool Hidden;

    [DataField]
    public LocId HidePopup = "toggle-hide-layers-hide-popup";

    [DataField]
    public LocId ShowPopup = "toggle-hide-layers-show-popup";
}

public sealed partial class ToggleHideLayersEvent : InstantActionEvent;
