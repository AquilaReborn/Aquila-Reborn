using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.HideBodyLayers;

/// <summary>
/// Даёт геймеру действия, скрывающие группы слоёв гуманоида (хвост, уши, волосы и т.д.), пока предмет надет.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ToggleHideLayersClothingSystem))]
public sealed partial class ToggleHideLayersClothingComponent : Component
{
    /// <summary>
    /// Слоты, в которых предмет выдаёт действия.
    /// </summary>
    [DataField]
    public SlotFlags Slots = SlotFlags.OUTERCLOTHING | SlotFlags.HEAD;

    /// <summary>
    /// Независимо переключаемые наборы слоёв. У каждого своё действие. Например, один набор скрывает хвост и уши, другой волосы.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<ToggleHideLayersGroup> Groups = new();
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ToggleHideLayersGroup
{
    [DataField(required: true)]
    public HashSet<HumanoidVisualLayers> Layers = new();

    [DataField]
    public EntProtoId Action = "ActionToggleHideLayersSuit";

    [DataField]
    public LocId HidePopup = "toggle-hide-layers-hide-popup";

    [DataField]
    public LocId ShowPopup = "toggle-hide-layers-show-popup";

    [ViewVariables]
    public NetEntity? ActionEntity;

    [ViewVariables]
    public bool Hidden;
}

public sealed partial class ToggleHideLayersEvent : InstantActionEvent;
