using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Inventory.Components;

/// <summary>
/// Предмет, в контейнере которого лежит вещь, продолжающая работать как надетая.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StoredClothingComponent : Component
{
    /// <summary>
    /// Вещь, эффекты которой должны работать, как будто она надета.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Item;

    /// <summary>
    /// Компоненты, скопированные с этой вещи на предмет.
    /// </summary>
    [ViewVariables]
    public List<string> Components = new();
}
