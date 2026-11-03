using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.Client._Funkystation.VendingMachines.UI;

public sealed partial class VendingMachineKeypadMenu
{
    /// <summary>
    /// Возвращает подпись с ценой товара для покупателя. Null, если цену показывать не нужно.
    /// </summary>
    public Func<EntProtoId, InventoryType, string?>? GetPriceLabel;

    private void ApplyPrice(VendingMachineGridSlot slot, VendingMachineInventoryEntry entry)
    {
        slot.SetPrice(GetPriceLabel?.Invoke(entry.ID, entry.Type));
    }
}
