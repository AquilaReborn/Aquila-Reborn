using Content.Shared._Aquila.Administration;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Systems;

public sealed partial class StationSpawningSystem
{
    [Dependency] private readonly SharedHandsSystem _adminCustomGearHands = default!;
    [Dependency] private readonly SharedStorageSystem _adminCustomGearStorage = default!;
    [Dependency] private readonly SharedContainerSystem _adminCustomGearContainers = default!;
    [Dependency] private readonly EntityWhitelistSystem _adminCustomGearWhitelist = default!;

    internal bool TryEquipAdminCharacterCustomGear(EntityUid uid, IEquipmentLoadout gear, out int skipped)
    {
        skipped = 0;
        if (!InventorySystem.TryGetSlots(uid, out var slots))
            return false;

        var spawned = new List<EntityUid>();
        try
        {
            TryEquipAdminCustomInventory(uid, slots, gear, spawned, ref skipped);
            TryEquipAdminCustomHands(uid, gear, spawned, ref skipped);
            TryFillAdminCustomStorage(uid, gear, spawned, ref skipped);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to equip custom admin spawn gear for {ToPrettyString(uid)}: {exception}");
            return false;
        }
        finally
        {
            DropUncontainedAdminCustomGear(uid, spawned, ref skipped);
        }
    }

    private void TryEquipAdminCustomInventory(EntityUid uid, SlotDefinition[] slots, IEquipmentLoadout gear,
        List<EntityUid> spawned, ref int skipped)
    {
        foreach (var slot in slots)
        {
            if (!gear.Equipment.TryGetValue(slot.Name, out var prototype))
                continue;

            if (!InventorySystem.TryGetSlotContainer(uid, slot.Name, out var container, out _)
                || container.ContainedEntity != null)
            {
                skipped += 1;
                continue;
            }

            var item = SpawnAdminCustomGearItem(uid, prototype, spawned);
            if ((slot.Whitelist != null && !_adminCustomGearWhitelist.IsWhitelistPass(slot.Whitelist, item))
                || !InventorySystem.TryEquip(uid, item, slot.Name, silent: true, force: true))
            {
                SkipAdminCustomGearItem(item, spawned, ref skipped);
            }
        }
    }

    private void TryEquipAdminCustomHands(EntityUid uid, IEquipmentLoadout gear, List<EntityUid> spawned,
        ref int skipped)
    {
        foreach (var prototype in gear.Inhand)
        {
            if (!_adminCustomGearHands.TryGetEmptyHand((uid, null), out var hand))
            {
                skipped += 1;
                continue;
            }

            var item = SpawnAdminCustomGearItem(uid, prototype, spawned);
            if (!_adminCustomGearHands.TryPickup(uid, item, hand, checkActionBlocker: false, animate: false)
                || _adminCustomGearHands.GetHeldItem((uid, null), hand) != item)
            {
                SkipAdminCustomGearItem(item, spawned, ref skipped);
            }
        }
    }

    private void TryFillAdminCustomStorage(EntityUid uid, IEquipmentLoadout gear, List<EntityUid> spawned,
        ref int skipped)
    {
        foreach (var (slot, contents) in gear.Storage)
        {
            if (contents.Count == 0)
                continue;

            if (!InventorySystem.TryGetSlotEntity(uid, slot, out var storageUid)
                || !TryComp<StorageComponent>(storageUid, out var storage))
            {
                skipped += contents.Count;
                continue;
            }

            foreach (var prototype in contents)
            {
                var item = SpawnAdminCustomGearItem(uid, prototype, spawned);
                if (!_adminCustomGearStorage.CanInsert(storageUid.Value, item, out _, storage, ignoreStacks: true)
                    || !_adminCustomGearStorage.Insert(storageUid.Value, item, out _, storageComp: storage,
                        playSound: false, stackAutomatically: false))
                {
                    SkipAdminCustomGearItem(item, spawned, ref skipped);
                }
            }
        }
    }

    private void SkipAdminCustomGearItem(EntityUid item, List<EntityUid> spawned, ref int skipped)
    {
        spawned.Remove(item);
        Del(item);
        skipped += 1;
    }

    private void DropUncontainedAdminCustomGear(EntityUid uid, List<EntityUid> spawned, ref int skipped)
    {
        foreach (var item in spawned)
        {
            if (IsAdminCustomGearContained(uid, item))
                continue;

            Del(item);
            skipped += 1;
        }
    }

    private EntityUid SpawnAdminCustomGearItem(EntityUid uid, EntProtoId prototype, List<EntityUid> spawned)
    {
        var item = SpawnAttachedTo(prototype, Transform(uid).Coordinates);
        spawned.Add(item);
        return item;
    }

    private bool IsAdminCustomGearContained(EntityUid uid, EntityUid item)
    {
        return !TerminatingOrDeleted(item)
            && !EntityManager.IsQueuedForDeletion(item)
            && TryComp(item, out TransformComponent? transform)
            && _adminCustomGearContainers.TryGetOuterContainer(item, transform, out var container)
            && container.Owner == uid;
    }

    private bool TryGetAdminCustomGearItem(EntityUid entity, out EntProtoId item)
    {
        item = default;
        if (MetaData(entity).EntityPrototype is not { } prototype)
            return false;

        item = new EntProtoId(prototype.ID);
        return AdminCharacterSpawnGearValidation.CanUseItem(prototype, EntityManager.ComponentFactory);
    }

    private bool TryGetAdminCustomGearEquipmentItem(EntityUid entity, out EntProtoId item)
    {
        item = default;
        if (MetaData(entity).EntityPrototype is not { } prototype)
            return false;

        item = new EntProtoId(prototype.ID);
        return AdminCharacterSpawnGearValidation.CanUseEquipmentItem(prototype, EntityManager.ComponentFactory);
    }

    internal AdminCharacterSpawnCustomGear? CaptureAdminCharacterCustomGear(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) || !InventorySystem.TryGetSlots(uid, out var slots))
            return null;

        var gear = new AdminCharacterSpawnCustomGear();
        var total = 0;

        foreach (var slot in slots)
        {
            if (!InventorySystem.TryGetSlotEntity(uid, slot.Name, out var slotEntity)
                || !TryGetAdminCustomGearEquipmentItem(slotEntity.Value, out var item))
            {
                continue;
            }

            total++;
            if (total > AdminCharacterSpawnGearValidation.MaxItems)
                return null;

            gear.Equipment[slot.Name] = item;

            if (!TryComp<StorageComponent>(slotEntity, out var storage))
                continue;

            foreach (var contained in storage.Container.ContainedEntities)
            {
                if (!TryGetAdminCustomGearItem(contained, out var containedItem))
                    continue;

                total++;
                if (total > AdminCharacterSpawnGearValidation.MaxItems)
                    return null;

                if (!gear.Storage.TryGetValue(slot.Name, out var contents))
                    gear.Storage[slot.Name] = contents = new List<EntProtoId>();

                contents.Add(containedItem);
            }
        }

        if (TryComp<HandsComponent>(uid, out var hands))
        {
            foreach (var held in _adminCustomGearHands.EnumerateHeld((uid, hands)))
            {
                if (!TryGetAdminCustomGearItem(held, out var item))
                    continue;

                total++;
                if (total > AdminCharacterSpawnGearValidation.MaxItems)
                    return null;

                gear.Inhand.Add(item);
            }
        }

        if (total == 0)
            return null;

        return AdminCharacterSpawnGearValidation.CanUseCustomGear(gear, _prototypeManager, EntityManager.ComponentFactory)
            ? gear
            : null;
    }
}
