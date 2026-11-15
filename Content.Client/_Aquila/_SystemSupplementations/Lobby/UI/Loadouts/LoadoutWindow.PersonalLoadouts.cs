using System.Linq;
using Content.Corvax.Interfaces.Shared;
using Content.Shared._Aquila.PersonalLoadouts;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.Loadouts;

public sealed partial class LoadoutWindow
{
    private static readonly ProtoId<LoadoutGroupPrototype> PersonalInventoryGroup = "Inventory";

    private static bool IsPersonalGroupEmpty(
        ProtoId<LoadoutGroupPrototype> group,
        RoleLoadout loadout,
        IDependencyCollection collection,
        IPrototypeManager protoManager)
    {
        return group == PersonalInventoryGroup
               && collection.TryResolveType<ISharedLoadoutsManager>(out var loadoutsManager)
               && !loadoutsManager.GetClientPrototypes()
                   .Any(id => protoManager.TryIndex<LoadoutPrototype>(id, out var personal)
                              && PersonalLoadoutEffect.IsAvailableForRole(personal, loadout.Role));
    }
}
