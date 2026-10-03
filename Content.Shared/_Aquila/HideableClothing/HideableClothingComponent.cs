using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.HideableClothing;

/// <summary>
/// Верхняя одежда, которую можно скрыть визуально, не снимая, пока под ней надет комбез. В основном только для броников.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(HideableClothingSystem), Other = AccessPermissions.Read)]
public sealed partial class HideableClothingComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionToggleClothingVisibility";

    /// <summary>
    /// Выданное носителю действие. Создаётся и хранится только на сервере.
    /// </summary>
    [ViewVariables]
    public EntityUid? ActionEntity;

    [ViewVariables, AutoNetworkedField]
    public bool Hidden;
}
