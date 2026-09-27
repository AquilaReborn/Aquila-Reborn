using Content.Goobstation.Common.Clothing;
using Content.Client.Inventory;
using Content.Shared._Aquila.HideableClothing;
using Content.Shared.Inventory;
using Content.Shared.Item;

namespace Content.Client._Aquila.HideableClothing;

public sealed class HideableClothingVisualsSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventorySlotsComponent, CheckClothingSlotHiddenEvent>(OnCheckHidden);
        SubscribeLocalEvent<HideableClothingComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnCheckHidden(Entity<InventorySlotsComponent> ent, ref CheckClothingSlotHiddenEvent args)
    {
        if (!args.Visible)
            return;

        if (!_inventory.TryGetSlotEntity(ent, args.Slot, out var item))
            return;

        if (TryComp<HideableClothingComponent>(item, out var hideable) && hideable.Hidden)
            args.Visible = false;
    }

    private void OnAfterState(Entity<HideableClothingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _item.VisualsChanged(ent);
    }
}
