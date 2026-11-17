using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Lobby;
using Content.Client.Station;
using Content.Shared._Aquila.Administration;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Administration;

/// <summary>
/// Создаёт предпросмотр персонажа с выбранным профилем, пресетом и снаряжением.
/// </summary>
public sealed class AdminCharacterSpawnUIController : UIController
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <summary>
    /// Создаёт куклу для предпросмотра, применяет к ней профиль, пресет и снаряжение или свой комплект.
    /// </summary>
    internal EntityUid CreatePreview(HumanoidCharacterProfile profile, ProtoId<HumanoidProfilePrototype>? presetId,
        ProtoId<StartingGearPrototype>? gearId, AdminCharacterSpawnCustomGear? customGear)
    {
        var dummy = UIManager.GetUIController<LobbyUIController>().LoadProfileEntity(profile, null, false);
        if (_prototypes.Resolve(presetId, out var preset)
            && _entities.TryGetComponent<HumanoidAppearanceComponent>(dummy, out var humanoid)
            && _entities.TryGetComponent<SpriteComponent>(dummy, out var sprite))
        {
            foreach (var (layer, info) in preset.CustomBaseLayers)
                humanoid.CustomBaseLayers[layer] = info;

            _entities.System<HumanoidAppearanceSystem>().UpdateSprite((dummy, humanoid, sprite));
        }

        var inventory = _entities.System<ClientInventorySystem>();
        if (customGear != null)
        {
            EquipSlots(dummy, customGear.Equipment, inventory);
            EquipHands(dummy, customGear.Inhand);
        }
        else if (_prototypes.Resolve(gearId, out var gear))
        {
            _entities.System<StationSpawningSystem>().EquipAdminCharacterSpawnLoadout(dummy, gear, profile, null);
            EquipSlots(dummy, gear.Equipment, inventory);
        }

        return dummy;
    }

    private void EquipSlots(EntityUid dummy, Dictionary<string, EntProtoId> equipment, ClientInventorySystem inventory)
    {
        if (!inventory.TryGetSlots(dummy, out var slots))
            return;

        var whitelist = _entities.System<EntityWhitelistSystem>();
        foreach (var slot in slots)
        {
            if (inventory.TryGetSlotEntity(dummy, slot.Name, out _)
                || !equipment.TryGetValue(slot.Name, out var prototype))
                continue;

            if (SpawnItem(prototype) is not { } item)
                continue;

            if ((slot.Whitelist != null && !whitelist.IsWhitelistPass(slot.Whitelist, item))
                || !inventory.TryEquip(dummy, item, slot.Name, silent: true, force: true))
            {
                _entities.DeleteEntity(item);
            }
        }
    }

    private void EquipHands(EntityUid dummy, List<EntProtoId> inhand)
    {
        var hands = _entities.System<SharedHandsSystem>();
        foreach (var prototype in inhand)
        {
            if (SpawnItem(prototype) is not { } item)
                continue;

            if (!hands.TryPickupAnyHand(dummy, item, checkActionBlocker: false, animate: false))
                _entities.DeleteEntity(item);
        }
    }

    private EntityUid? SpawnItem(EntProtoId prototype)
    {
        if (string.IsNullOrEmpty(prototype.Id) || !_prototypes.TryIndex(prototype, out EntityPrototype? _))
            return null;

        return _entities.Spawn(prototype, MapCoordinates.Nullspace);
    }
}
