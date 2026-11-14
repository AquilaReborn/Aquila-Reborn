using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;

namespace Content.Server.Station.Systems;

public sealed partial class StationSpawningSystem
{
    private bool TryEquipAdminCharacterSpawnGear(EntityUid uid, StartingGearPrototype? gear,
        Func<EntityUid, bool>? placeBeforeEquipment, HumanoidCharacterProfile? profile)
    {
        if (placeBeforeEquipment?.Invoke(uid) == false)
            return false;

        if (gear == null)
            return true;

        EquipAdminCharacterSpawnLoadout(uid, gear, profile, _actors.GetSession(uid));
        EquipStartingGear(uid, gear, raiseEvent: false);
        return true;
    }

    private void ApplyAdminPresetLayers(EntityUid uid, HumanoidProfilePrototype? preset)
    {
        if (preset == null || !TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
            return;

        foreach (var (layer, info) in preset.CustomBaseLayers)
            appearance.CustomBaseLayers[layer] = info;
    }
}
