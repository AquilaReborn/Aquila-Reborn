using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Lobby;
using Content.Client.Station;
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
    /// Создаёт куклу для предпросмотра, применяет к ней профиль, пресет и снаряжение.
    /// </summary>
    internal EntityUid CreatePreview(HumanoidCharacterProfile profile, ProtoId<HumanoidProfilePrototype>? presetId,
        ProtoId<StartingGearPrototype>? gearId)
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
        if (_prototypes.Resolve(gearId, out var gear))
        {
            _entities.System<StationSpawningSystem>().EquipAdminCharacterSpawnLoadout(dummy, gear, profile, null);

            if (inventory.TryGetSlots(dummy, out var slots))
            {
                var whitelist = _entities.System<EntityWhitelistSystem>();
                foreach (var slot in slots)
                {
                    if (inventory.TryGetSlotEntity(dummy, slot.Name, out _))
                        continue;

                    if (!gear.Equipment.TryGetValue(slot.Name, out var prototype))
                        continue;

                    var item = _entities.SpawnEntity(prototype, MapCoordinates.Nullspace);
                    if ((slot.Whitelist != null && !whitelist.IsWhitelistPass(slot.Whitelist, item))
                        || !inventory.TryEquip(dummy, item, slot.Name, silent: true, force: true))
                    {
                        _entities.DeleteEntity(item);
                    }
                }
            }
        }

        return dummy;
    }
}
