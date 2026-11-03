using Content.Client._Aquila.Economy;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.Client.VendingMachines;

public sealed partial class VendingMachineBoundUserInterface
{
    private string? GetPriceLabel(EntityUid player, EntProtoId item, InventoryType type)
    {
        return EntMan.System<PaidVendingMachineSystem>().GetPriceLabel(Owner, player, item, type);
    }
}
