using Content.Shared._Aquila.Economy;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem
{
    private bool IsPurchaseCancelled(Entity<VendingMachineComponent> vending, EntityUid? user, string itemId, InventoryType type, bool cleanedReturnedInventory)
    {
        if (user is not { } buyer)
            return false;

        var purchase = new VendingMachinePurchaseAttemptEvent(buyer, itemId, type);
        RaiseLocalEvent(vending, ref purchase);
        if (!purchase.Cancelled)
            return false;

        if (cleanedReturnedInventory)
        {
            Dirty(vending);
            UpdateUI(vending.AsNullable());
        }

        return true;
    }

    private bool IsAuthorizedByEvent(EntityUid uid, EntityUid sender)
    {
        var authorize = new VendingMachineAuthorizeEvent(sender);
        RaiseLocalEvent(uid, ref authorize);
        return authorize.Authorized;
    }
}
