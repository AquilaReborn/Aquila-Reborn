using Content.Client._Aquila.Economy;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.Client.VendingMachines;

public sealed partial class VendingMachineBoundUserInterface
{
    private string? GetPriceLabel(EntityUid player, EntProtoId item, InventoryType type)
    {
        var paid = EntMan.System<PaidVendingMachineSystem>();
        if (!paid.CanBuy(Owner, player, item, type))
            return Loc.GetString("vending-paid-restricted-label");

        var price = paid.GetPriceFor(Owner, player, item);
        return price > 0 ? Loc.GetString("vending-paid-price", ("price", price)) : null;
    }
}
