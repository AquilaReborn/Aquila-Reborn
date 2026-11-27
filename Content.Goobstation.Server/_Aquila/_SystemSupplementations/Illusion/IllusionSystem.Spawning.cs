using Content.Goobstation.Shared.Illusion;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Hands.Components;
using Content.Shared.Heretic.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Illusion;

public sealed partial class IllusionSystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ToggleableClothingSystem _toggleable = default!;

    private void OnSpawnIllusion(ref SpawnIllusionEvent args)
    {
        if (args.Count <= 0 || args.Lifetime <= TimeSpan.Zero || args.HealthMultiplier <= 0f ||
            !CanSpawnIndependentIllusion(args.Source))
            return;

        var lifetime = (float) args.Lifetime.TotalSeconds;
        var targets = args.Target is { } target ? new[] { target } : Array.Empty<EntityUid>();

        for (var i = 0; i < args.Count; i++)
        {
            if (!ReserveIllusionSlot(ref args, out var budget))
                break;

            EntityUid? clone = null;
            try
            {
                if (args.Targets is { Count: > 0 } targetList)
                    targets = new[] { targetList[i % targetList.Count] };

                clone = SpawnIllusion(args.Source, lifetime, args.HealthMultiplier, targets, new ComponentRegistry());
                if (clone is { } budgeted && budget != null && args.BudgetOwner is { } owner)
                    RegisterBudgetedIllusion(budgeted, owner, budget);
            }
            catch (Exception e)
            {
                if (clone is { } failed && !TerminatingOrDeleted(failed))
                    QueueDel(failed);

                Log.Error($"Failed to spawn an illusion of {ToPrettyString(args.Source)}: {e}");
                continue;
            }
            finally
            {
                if (budget != null)
                    budget.Reservations--;
            }

            if (clone is not { } spawned || TerminatingOrDeleted(spawned) || EntityManager.IsQueuedForDeletion(spawned))
                continue;

            if (args.ExplosionType is { } explosionType && TryComp<IllusionComponent>(spawned, out var illusion))
            {
                illusion.ExplosionType = explosionType;

                if (args.ExplosionRadius is { } explosionRadius)
                    illusion.ExplosionRadius = explosionRadius;
            }

            try
            {
                RestoreEquipment(args.Source, spawned);

                if (TryComp<MoonRegaliaWearerComponent>(spawned, out var regalia) && regalia.GrantedPacified)
                    RemComp<PacifiedComponent>(spawned);

                RemComp<MoonRegaliaWearerComponent>(spawned);
                WeakenIllusion(spawned);
            }
            catch (Exception e)
            {
                QueueDel(spawned);
                Log.Error($"Failed to prepare an illusion of {ToPrettyString(args.Source)}: {e}");
            }
        }
    }

    private void RestoreEquipment(EntityUid source, EntityUid clone)
    {
        if (!_prototype.Resolve(Settings, out var settings))
            return;

        var slots = _inventory.GetSlotEnumerator(source, SlotFlags.WITHOUT_POCKET);
        var coords = Transform(clone).Coordinates;

        while (slots.NextItem(out var item, out var slot))
        {
            if (_inventory.TryGetSlotEntity(clone, slot.Name, out _))
                continue;

            if (_cloning.CopyItem(item, coords, settings.Whitelist, settings.Blacklist, copyStorage: false) is not { } copy)
                continue;

            if (!_inventory.TryEquip(clone, copy, slot.Name, silent: true, force: true))
            {
                QueueDel(copy);
                continue;
            }

            if (settings.MakeEquipmentUnremoveable)
                EnsureComp<UnremoveableComponent>(copy);

            MirrorToggledClothing(item, copy, clone);
        }
    }

    private void MirrorToggledClothing(EntityUid sourceItem, EntityUid cloneItem, EntityUid clone)
    {
        if (!TryComp<ToggleableClothingComponent>(sourceItem, out var toggleable) ||
            toggleable.ClothingUids.Count == 0 ||
            !TryComp<ToggleableClothingComponent>(cloneItem, out var cloneToggleable))
            return;

        foreach (var (clothing, slot) in toggleable.ClothingUids)
        {
            if (!_toggleable.IsToggled((sourceItem, toggleable), clothing))
                continue;

            EntityUid? attached = null;
            foreach (var (candidate, candidateSlot) in cloneToggleable.ClothingUids)
            {
                if (candidateSlot != slot)
                    continue;

                attached = candidate;
                break;
            }

            if (attached is { } attachedUid)
                _inventory.TryEquip(clone, attachedUid, slot, silent: true, force: true);
        }
    }

    private void WeakenIllusion(EntityUid clone)
    {
        if (TryComp<MeleeWeaponComponent>(clone, out var unarmed))
            ScaleMeleeDamage(clone, unarmed);

        if (!TryComp<HandsComponent>(clone, out var hands))
            return;

        foreach (var held in _hands.EnumerateHeld((clone, hands)))
        {
            if (HasComp<GunComponent>(held))
            {
                QueueDel(held);
                continue;
            }

            RemComp<HereticBladeComponent>(held);

            if (TryComp<MeleeWeaponComponent>(held, out var melee))
                ScaleMeleeDamage(held, melee);
        }
    }

    private void ScaleMeleeDamage(EntityUid uid, MeleeWeaponComponent melee)
    {
        var total = melee.Damage.GetTotal().Float();
        if (total <= 0f)
            return;

        melee.Damage *= 1f / total;
        DirtyField(uid, melee, nameof(MeleeWeaponComponent.Damage));
    }
}
