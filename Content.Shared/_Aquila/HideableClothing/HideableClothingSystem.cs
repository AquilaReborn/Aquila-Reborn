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

    private const SlotFlags RelevantSlots = SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<InventoryComponent, DidUnequipEvent>(OnDidUnequip);

        SubscribeLocalEvent<HideableClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<HideableClothingComponent, ToggleClothingVisibilityEvent>(OnToggle);
    }

    private void OnDidEquip(Entity<InventoryComponent> ent, ref DidEquipEvent args)
    {
        if ((args.SlotFlags & RelevantSlots) != 0)
            RefreshAction(ent);
    }

    private void OnDidUnequip(Entity<InventoryComponent> ent, ref DidUnequipEvent args)
    {
        if ((args.SlotFlags & RelevantSlots) != 0)
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

        args.Handled = true;
        SetHidden(ent, !ent.Comp.Hidden);
    }

    private void SetHidden(Entity<HideableClothingComponent> ent, bool hidden)
    {
        if (ent.Comp.Hidden == hidden)
            return;

        ent.Comp.Hidden = hidden;
        Dirty(ent);

        _item.VisualsChanged(ent);
    }

    /// <summary>
    /// Выдаёт действие, пока под верхней одеждой есть комбез, иначе забирает его и возвращает видимость.
    /// </summary>
    private void RefreshAction(EntityUid wearer)
    {
        if (!TryGetOuterClothing(wearer, out var outer))
            return;

        var hasInner = HasInnerClothing(wearer);

        // Действия создаёт и забирает только сервер, клиент получает их через состояние.
        if (_net.IsServer)
        {
            if (hasInner)
                _actions.AddAction(wearer, ref outer.Comp.ActionEntity, outer.Comp.Action, outer.Owner);
            else
                _actions.RemoveAction(outer.Comp.ActionEntity);
        }

        if (!hasInner)
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
        return _inventory.TryGetContainerSlotEnumerator(wearer, out var enumerator, SlotFlags.INNERCLOTHING)
               && enumerator.NextItem(out _);
    }
}
