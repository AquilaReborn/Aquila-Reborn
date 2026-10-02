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
    private const int MainGroup = -1;

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

        EnsureExtraState(ent);

        if (HasAnyLayer(args.User, ent.Comp.Layers))
            args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);

        for (var i = 0; i < ent.Comp.ExtraToggles.Count; i++)
        {
            var group = ent.Comp.ExtraToggles[i];
            if (!HasAnyLayer(args.User, group.Layers))
                continue;

            EntityUid? action = ent.Comp.ExtraActionEntities[i] == EntityUid.Invalid
                ? null
                : ent.Comp.ExtraActionEntities[i];

            args.AddAction(ref action, group.Action);
            ent.Comp.ExtraActionEntities[i] = action ?? EntityUid.Invalid;
        }

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

        EnsureExtraState(ent);

        var group = FindGroup(ent.Comp, args.Action.Owner);
        var show = IsAnyLayerHidden(args.Performer, GetLayers(ent.Comp, group));
        SetHidden(ent, args.Performer, group, !show, show);

        var popup = GetHiddenFlag(ent.Comp, group) ? GetHidePopup(ent.Comp, group) : GetShowPopup(ent.Comp, group);
        _popup.PopupClient(Loc.GetString(popup, ("item", ent.Owner)), args.Performer, args.Performer);
    }

    private void OnGotEquipped(Entity<ToggleHideLayersClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryComp<ClothingComponent>(ent, out var clothing)
            || clothing.InSlotFlag is not { } slot
            || (slot & ent.Comp.Slots) == 0)
            return;

        EnsureExtraState(ent);

        if (ent.Comp.HiddenByDefault)
            SetHidden(ent, args.Wearer, MainGroup, true);

        for (var i = 0; i < ent.Comp.ExtraToggles.Count; i++)
        {
            if (ent.Comp.ExtraToggles[i].HiddenByDefault)
                SetHidden(ent, args.Wearer, i, true);
        }
    }

    private void OnGotUnequipped(Entity<ToggleHideLayersClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        EnsureExtraState(ent);

        SetHidden(ent, args.Wearer, MainGroup, false);

        for (var i = 0; i < ent.Comp.ExtraToggles.Count; i++)
        {
            SetHidden(ent, args.Wearer, i, false);
        }
    }

    private void EnsureExtraState(Entity<ToggleHideLayersClothingComponent> ent)
    {
        var count = ent.Comp.ExtraToggles.Count;
        var changed = false;

        while (ent.Comp.ExtraActionEntities.Count < count)
        {
            ent.Comp.ExtraActionEntities.Add(EntityUid.Invalid);
            changed = true;
        }

        while (ent.Comp.ExtraHidden.Count < count)
        {
            ent.Comp.ExtraHidden.Add(false);
            changed = true;
        }

        if (changed)
            Dirty(ent);
    }

    private static int FindGroup(ToggleHideLayersClothingComponent comp, EntityUid action)
    {
        if (comp.ActionEntity == action)
            return MainGroup;

        for (var i = 0; i < comp.ExtraActionEntities.Count; i++)
        {
            if (comp.ExtraActionEntities[i] == action)
                return i;
        }

        return MainGroup;
    }

    private static HashSet<HumanoidVisualLayers> GetLayers(ToggleHideLayersClothingComponent comp, int group)
    {
        return group == MainGroup ? comp.Layers : comp.ExtraToggles[group].Layers;
    }

    private static bool GetHiddenFlag(ToggleHideLayersClothingComponent comp, int group)
    {
        return group == MainGroup ? comp.Hidden : comp.ExtraHidden[group];
    }

    private static LocId GetHidePopup(ToggleHideLayersClothingComponent comp, int group)
    {
        return group == MainGroup ? comp.HidePopup : comp.ExtraToggles[group].HidePopup;
    }

    private static LocId GetShowPopup(ToggleHideLayersClothingComponent comp, int group)
    {
        return group == MainGroup ? comp.ShowPopup : comp.ExtraToggles[group].ShowPopup;
    }

    private void SetHidden(Entity<ToggleHideLayersClothingComponent> ent, EntityUid wearer, int group, bool hidden, bool force = false)
    {
        if (!force && GetHiddenFlag(ent.Comp, group) == hidden)
            return;

        if (GetHiddenFlag(ent.Comp, group) != hidden)
        {
            if (group == MainGroup)
                ent.Comp.Hidden = hidden;
            else
                ent.Comp.ExtraHidden[group] = hidden;

            Dirty(ent);
        }

        foreach (var layer in GetLayers(ent.Comp, group))
        {
            if (!hidden && IsHiddenByOther(wearer, ent.Owner, group, layer))
                continue;

            if (!hidden && force)
                ClearHideSources(wearer, layer);

            _humanoid.SetLayerVisibility(wearer, layer, !hidden, HideSource);
        }
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

    private bool IsHiddenByOther(EntityUid wearer, EntityUid exceptItem, int exceptGroup, HumanoidVisualLayers layer)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(wearer, out var enumerator))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (!TryComp<ToggleHideLayersClothingComponent>(item, out var other))
                continue;

            if ((item != exceptItem || exceptGroup != MainGroup)
                && other.Hidden
                && other.Layers.Contains(layer))
                return true;

            for (var i = 0; i < other.ExtraToggles.Count && i < other.ExtraHidden.Count; i++)
            {
                if ((item == exceptItem && i == exceptGroup) || !other.ExtraHidden[i])
                    continue;

                if (other.ExtraToggles[i].Layers.Contains(layer))
                    return true;
            }
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
