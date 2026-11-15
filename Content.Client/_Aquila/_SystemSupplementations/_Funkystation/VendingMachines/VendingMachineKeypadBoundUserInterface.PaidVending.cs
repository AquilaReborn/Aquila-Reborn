using Content.Client._Aquila.Economy;
using Content.Client._Funkystation.VendingMachines.UI;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared.VendingMachines;

namespace Content.Client._Funkystation.VendingMachines;

public sealed partial class VendingMachineKeypadBoundUserInterface
{
    private void SetupPaidVending(VendingMachineKeypadMenu menu)
    {
        if (!EntMan.HasComponent<PaidVendingMachineComponent>(Owner) || PlayerManager.LocalEntity is not { } player)
            return;

        var paid = EntMan.System<PaidVendingMachineSystem>();
        menu.GetPriceLabel = (item, type) => paid.GetPriceLabel(Owner, player, item, type);
    }

    private bool IsAuthorizedByEvent(EntityUid player)
    {
        var ev = new VendingMachineAuthorizeEvent(player);
        EntMan.EventBus.RaiseLocalEvent(Owner, ref ev);
        return ev.Authorized;
    }

    private bool CanBuy(EntityUid player, VendingMachineInventoryEntry entry)
    {
        return !EntMan.HasComponent<PaidVendingMachineComponent>(Owner)
               || EntMan.System<PaidVendingMachineSystem>().CanBuy(Owner, player, entry.ID, entry.Type);
    }
}
