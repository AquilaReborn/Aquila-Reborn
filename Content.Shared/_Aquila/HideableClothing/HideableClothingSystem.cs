using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Shared.Network;

namespace Content.Shared._Aquila.HideableClothing;

public sealed class HideableClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, DidEquipEvent>(OnAnyEquip);
        SubscribeLocalEvent<InventoryComponent, DidUnequipEvent>(OnAnyUnequip);

        SubscribeLocalEvent<HideableClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<HideableClothingComponent, ToggleClothingVisibilityEvent>(OnToggle);
    }

    #region Event handlers

    private void OnAnyEquip(Entity<InventoryComponent> ent, ref DidEquipEvent args)
    {
        RefreshAction(ent);
    }

    private void OnAnyUnequip(Entity<InventoryComponent> ent, ref DidUnequipEvent args)
    {
        RefreshAction(ent);
    }

    private void OnGotUnequipped(Entity<HideableClothingComponent> ent, ref GotUnequippedEvent args)
    {
        SetHidden(ent, false);
    }

    private void OnToggle(Entity<HideableClothingComponent> ent, ref ToggleClothingVisibilityEvent args)
    {
        if (args.Handled)
            return;

        SetHidden(ent, !ent.Comp.Hidden);
        args.Handled = true;
    }

    #endregion

    #region Helpers

    private void SetHidden(Entity<HideableClothingComponent> ent, bool hidden)
    {
        if (ent.Comp.Hidden == hidden)
            return;

        ent.Comp.Hidden = hidden;
        Dirty(ent);

        _item.VisualsChanged(ent);
    }

    private void RefreshAction(EntityUid wearer)
    {
        if (!TryGetOuterClothing(wearer, out var outer))
            return;

        if (HasInnerClothing(wearer))
        {
            if (_net.IsServer)
                _actions.AddAction(wearer, ref outer.Comp.ActionEntity, outer.Comp.ActionId, outer.Owner);
            return;
        }

        if (_net.IsServer
            && _actions.GetAction(outer.Comp.ActionEntity, false) is { } action
            && action.Comp.AttachedEntity == wearer)
        {
            _actions.RemoveAction(wearer, outer.Comp.ActionEntity);
        }

        SetHidden(outer, false);
    }

    private bool TryGetOuterClothing(EntityUid wearer, out Entity<HideableClothingComponent> outer)
    {
        outer = default;

        if (!_inventory.TryGetContainerSlotEnumerator(wearer, out var enumerator, SlotFlags.OUTERCLOTHING))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (!TryComp<HideableClothingComponent>(item, out var comp))
                continue;

            outer = (item, comp);
            return true;
        }

        return false;
    }

    private bool HasInnerClothing(EntityUid wearer)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(wearer, out var enumerator, SlotFlags.INNERCLOTHING))
            return false;

        return enumerator.NextItem(out _);
    }

    #endregion
}
