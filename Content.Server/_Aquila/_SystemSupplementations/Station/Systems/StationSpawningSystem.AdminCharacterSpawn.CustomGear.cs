using System.Linq;
using Content.Shared._Aquila.Administration;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Systems;

public sealed partial class StationSpawningSystem
{
    [Dependency] private readonly SharedHandsSystem _adminCustomGearHands = default!;
    [Dependency] private readonly SharedStorageSystem _adminCustomGearStorage = default!;
    [Dependency] private readonly SharedContainerSystem _adminCustomGearContainers = default!;

    internal bool TryEquipAdminCharacterCustomGear(EntityUid uid, IEquipmentLoadout gear, out List<EntityUid> spawned)
    {
        spawned = new List<EntityUid>();
        var equipped = false;
        try
        {
            equipped = TryEquipAdminCustomInventory(uid, gear, spawned)
                && TryEquipAdminCustomHands(uid, gear, spawned)
                && TryFillAdminCustomStorage(uid, gear, spawned)
                && spawned.All(item => IsAdminCustomGearContained(uid, item));
            return equipped;
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to equip custom admin spawn gear for {ToPrettyString(uid)}: {exception}");
            return false;
        }
        finally
        {
            if (!equipped)
            {
                for (var i = spawned.Count - 1; i >= 0; i--)
                    Del(spawned[i]);
            }
        }
    }

    private bool TryEquipAdminCustomInventory(EntityUid uid, IEquipmentLoadout gear, List<EntityUid> spawned)
    {
        if (gear.Equipment.Count == 0)
            return true;

        if (!InventorySystem.TryGetSlots(uid, out var slots))
            return false;

        foreach (var slot in gear.Equipment.Keys)
        {
            if (!InventorySystem.TryGetSlotContainer(uid, slot, out var container, out _)
                || container.ContainedEntity != null)
            {
                return false;
            }
        }

        var pending = slots.Where(slot => gear.Equipment.ContainsKey(slot.Name)).ToList();
        while (pending.Count > 0)
        {
            var before = pending.Count;
            for (var index = 0; index < pending.Count;)
            {
                var slot = pending[index];
                if (slot.DependsOn is { } required && !InventorySystem.TryGetSlotEntity(uid, required, out _))
                {
                    index++;
                    continue;
                }

                var item = SpawnAdminCustomGearItem(uid, gear.Equipment[slot.Name], spawned);
                if (!InventorySystem.TryEquip(uid, item, slot.Name, silent: true))
                    return false;

                pending.RemoveAt(index);
            }

            if (pending.Count == before)
                return false;
        }

        return true;
    }

    private bool TryEquipAdminCustomHands(EntityUid uid, IEquipmentLoadout gear, List<EntityUid> spawned)
    {
        foreach (var prototype in gear.Inhand)
        {
            if (!_adminCustomGearHands.TryGetEmptyHand((uid, null), out var hand))
                return false;

            var item = SpawnAdminCustomGearItem(uid, prototype, spawned);
            if (!_adminCustomGearHands.TryPickup(uid, item, hand, checkActionBlocker: false, animate: false)
                || _adminCustomGearHands.GetHeldItem((uid, null), hand) != item)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryFillAdminCustomStorage(EntityUid uid, IEquipmentLoadout gear, List<EntityUid> spawned)
    {
        foreach (var (slot, contents) in gear.Storage)
        {
            if (contents.Count == 0)
                continue;

            if (!InventorySystem.TryGetSlotEntity(uid, slot, out var storageUid)
                || !TryComp<StorageComponent>(storageUid, out var storage))
            {
                return false;
            }

            foreach (var prototype in contents)
            {
                var item = SpawnAdminCustomGearItem(uid, prototype, spawned);
                if (!_adminCustomGearStorage.CanInsert(storageUid.Value, item, out _, storage, ignoreStacks: true)
                    || !_adminCustomGearStorage.Insert(storageUid.Value, item, out _, storageComp: storage,
                        playSound: false, stackAutomatically: false))
                {
                    return false;
                }
            }
        }

        return true;
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
        return AdminCharacterSpawnGearValidation.CanUseItem(item, _prototypeManager, EntityManager.ComponentFactory);
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
                || !TryGetAdminCustomGearItem(slotEntity.Value, out var item))
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
