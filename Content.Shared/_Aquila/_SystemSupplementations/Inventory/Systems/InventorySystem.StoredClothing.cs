using Content.Shared._Aquila.Inventory.Components;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    /// <summary>
    /// Передаёт инвентарный релей вещи, лежащей в контейнере предмета, как будто она надета.
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
