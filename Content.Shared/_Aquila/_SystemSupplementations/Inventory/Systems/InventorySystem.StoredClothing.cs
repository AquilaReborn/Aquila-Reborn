using Content.Shared._Aquila.Inventory.Components;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    /// <summary>
    /// Relays an inventory event to an item stored in the item's container as if it were worn.
    /// </summary>
    private void RelayStoredClothing<T>(EntityUid item, InventoryRelayedEvent<T> ev) where T : IInventoryRelayEvent
    {
        if (!TryComp<StoredClothingComponent>(item, out var stored)
            || stored.Item is not { } storedItem
            || TerminatingOrDeleted(storedItem))
            return;

        RaiseLocalEvent(storedItem, ev);
    }
}
