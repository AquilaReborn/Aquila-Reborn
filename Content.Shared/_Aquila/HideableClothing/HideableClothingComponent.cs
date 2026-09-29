using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.HideableClothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HideableClothingComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionToggleClothingVisibility";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public bool Hidden;
}
