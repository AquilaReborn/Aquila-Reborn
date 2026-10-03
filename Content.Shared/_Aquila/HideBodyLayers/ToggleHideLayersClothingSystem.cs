using System.Numerics;
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
        SubscribeLocalEvent<ToggleHideLayersClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGetActions(Entity<ToggleHideLayersClothingComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags is not { } slot || (slot & ent.Comp.Slots) == 0)
            return;

        foreach (var group in ent.Comp.Groups)
        {
            if (!HasAnyLayer(args.User, group.Layers))
                continue;

            var action = GetEntity(group.ActionEntity);
            args.AddAction(ref action, group.Action);
            group.ActionEntity = GetNetEntity(action);
        }

        Dirty(ent);
    }

    private void OnToggle(Entity<ToggleHideLayersClothingComponent> ent, ref ToggleHideLayersEvent args)
    {
        if (args.Handled || !IsWornInSlot(ent))
            return;

        args.Handled = true;

        if (FindGroup(ent.Comp, args.Action.Owner) is not { } group)
            return;

        var reveal = IsAnyLayerHidden(args.Performer, group.Layers);
        SetHidden(ent, args.Performer, group, !reveal, reveal);

        var popup = group.Hidden ? group.HidePopup : group.ShowPopup;
        _popup.PopupClient(Loc.GetString(popup, ("item", ent.Owner)), args.Performer, args.Performer);
    }

    private void OnGotUnequipped(Entity<ToggleHideLayersClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        foreach (var group in ent.Comp.Groups)
        {
            SetHidden(ent, args.Wearer, group, false);
        }
    }

    private bool IsWornInSlot(Entity<ToggleHideLayersClothingComponent> ent)
    {
        return TryComp<ClothingComponent>(ent, out var clothing)
               && clothing.InSlotFlag is { } slot
               && (slot & ent.Comp.Slots) != 0;
    }

    private ToggleHideLayersGroup? FindGroup(ToggleHideLayersClothingComponent comp, EntityUid action)
    {
        var netAction = GetNetEntity(action);

        foreach (var group in comp.Groups)
        {
            if (group.ActionEntity == netAction)
                return group;
        }

        return null;
    }

    /// <param name="force">чтобы не конфликтовало с другой системой, которая скрывает слои</param>
    private void SetHidden(
        Entity<ToggleHideLayersClothingComponent> ent,
        EntityUid wearer,
        ToggleHideLayersGroup group,
        bool hidden,
        bool force = false)
    {
        if (group.Hidden != hidden)
        {
            group.Hidden = hidden;
            Dirty(ent);
        }
        else if (!force)
        {
            return;
        }

        foreach (var layer in group.Layers)
        {
            if (!hidden && IsHiddenByOther(wearer, group, layer))
                continue;

            if (!hidden && force)
                ClearHideSources(wearer, layer);

            _humanoid.SetLayerVisibility(wearer, layer, !hidden, HideSource);
        }
    }

    private bool IsHiddenByOther(EntityUid wearer, ToggleHideLayersGroup except, HumanoidVisualLayers layer)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(wearer, out var enumerator))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (!TryComp<ToggleHideLayersClothingComponent>(item, out var other))
                continue;

            foreach (var group in other.Groups)
            {
                if (group != except && group.Hidden && group.Layers.Contains(layer))
                    return true;
            }
        }

        return false;
    }

    private bool IsAnyLayerHidden(EntityUid uid, HashSet<HumanoidVisualLayers> layers)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return false;

        foreach (var layer in layers)
        {
            if (humanoid.HiddenLayers.ContainsKey(layer))
                return true;
        }

        return false;
    }

    private void ClearHideSources(EntityUid uid, HumanoidVisualLayers layer)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid)
            || !humanoid.HiddenLayers.TryGetValue(layer, out var sources))
            return;

        foreach (var flag in Enum.GetValues<SlotFlags>())
        {
            if (flag == HideSource
                || (sources & flag) == 0
                || !BitOperations.IsPow2((uint) flag))
                continue;

            _humanoid.SetLayerVisibility(uid, layer, true, flag);
        }
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
