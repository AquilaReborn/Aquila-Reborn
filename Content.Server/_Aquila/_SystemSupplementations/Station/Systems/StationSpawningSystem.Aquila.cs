using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;

namespace Content.Server.Station.Systems;

public sealed partial class StationSpawningSystem
{
    private bool TryEquipAdminSpawnGear(EntityUid uid, StartingGearPrototype? gear)
    {
        if (gear == null)
            return false;

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
