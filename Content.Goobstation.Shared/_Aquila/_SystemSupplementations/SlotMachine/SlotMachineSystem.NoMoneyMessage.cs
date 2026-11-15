using Content.Shared.Containers.ItemSlots;

namespace Content.Goobstation.Shared.SlotMachine;

public sealed partial class SlotMachineSystem
{
    private static readonly LocId NoMoneyMessage = "slotmachine-no-money-cost";

    private string GetNoMoneyMessage(Entity<SlotMachineComponent> ent, ItemSlot? slot)
    {
        var inserted = slot?.Item is { } item ? _stackSystem.GetCount(item) : 0;
        return Loc.GetString(NoMoneyMessage, ("cost", ent.Comp.SpinCost), ("inserted", inserted));
    }
}
