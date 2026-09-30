using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._Aquila.HideBodyLayers;

public sealed class ToggleHideLayersClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const SlotFlags HideSource = SlotFlags.PREVENTEQUIP;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleHideLayersClothingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<ToggleHideLayersClothingComponent, ToggleHideLayersEvent>(OnToggle);
        SubscribeLocalEvent<ToggleHideLayersClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ToggleHideLayersClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGetActions(Entity<ToggleHideLayersClothingComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags is not { } slot || (slot & ent.Comp.Slots) == 0)
            return;

        if (!HasAnyLayer(args.User, ent.Comp.Layers))
            return;

        args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
        Dirty(ent);
    }

    private void OnToggle(Entity<ToggleHideLayersClothingComponent> ent, ref ToggleHideLayersEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ClothingComponent>(ent, out var clothing)
            || clothing.InSlotFlag is not { } slot
            || (slot & ent.Comp.Slots) == 0)
            return;

        args.Handled = true;

        SetHidden(ent, args.Performer, !ent.Comp.Hidden);

        var popup = ent.Comp.Hidden ? ent.Comp.HidePopup : ent.Comp.ShowPopup;
        _popup.PopupClient(Loc.GetString(popup, ("item", ent.Owner)), args.Performer, args.Performer);
    }

    private void OnGotEquipped(Entity<ToggleHideLayersClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (_timing.ApplyingState || !ent.Comp.HiddenByDefault)
            return;

        if (!TryComp<ClothingComponent>(ent, out var clothing)
            || clothing.InSlotFlag is not { } slot
            || (slot & ent.Comp.Slots) == 0)
            return;

        SetHidden(ent, args.Wearer, true);
    }

    private void OnGotUnequipped(Entity<ToggleHideLayersClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        SetHidden(ent, args.Wearer, false);
    }

    private void SetHidden(Entity<ToggleHideLayersClothingComponent> ent, EntityUid wearer, bool hidden)
    {
        if (ent.Comp.Hidden == hidden)
            return;

        ent.Comp.Hidden = hidden;
        Dirty(ent);

        foreach (var layer in ent.Comp.Layers)
        {
            if (!hidden && IsHiddenByOther(wearer, ent.Owner, layer))
                continue;

            _humanoid.SetLayerVisibility(wearer, layer, !hidden, HideSource);
        }
    }

    private bool IsHiddenByOther(EntityUid wearer, EntityUid except, HumanoidVisualLayers layer)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(wearer, out var enumerator))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (item == except
                || !TryComp<ToggleHideLayersClothingComponent>(item, out var other)
                || !other.Hidden)
                continue;

            if (other.Layers.Contains(layer))
                return true;
        }

        return false;
    }

    private bool HasAnyLayer(EntityUid uid, HashSet<HumanoidVisualLayers> layers)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return false;

        foreach (var layer in layers)
        {
            if (humanoid.CustomBaseLayers.ContainsKey(layer))
                return true;
        }

        foreach (var markings in humanoid.MarkingSet.Markings.Values)
        {
            foreach (var marking in markings)
            {
                if (_markings.TryGetMarking(marking, out var proto) && layers.Contains(proto.BodyPart))
                    return true;
            }
        }

        return false;
    }
}
