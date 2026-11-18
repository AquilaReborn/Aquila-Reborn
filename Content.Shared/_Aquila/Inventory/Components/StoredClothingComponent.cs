using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Inventory.Components;

/// <summary>
/// Item that holds a stored item in its container which keeps working as if worn.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StoredClothingComponent : Component
{
    /// <summary>
    /// The item whose effects should work as if it were worn.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Item;

    /// <summary>
    /// Components copied from that item to this one.
    /// </summary>
    [ViewVariables]
    public List<string> Components = new();
}
