using Content.Shared.CCVar;
using Content.Shared.DetailExaminable;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Map;

namespace Content.Server.Station.Systems;

public sealed partial class StationSpawningSystem
{
    /// <summary>
    /// Spawns a humanoid body for the given profile and applies the appearance preset.
    /// </summary>
    /// <param name="coordinates">Coordinates to spawn the body at.</param>
    /// <param name="profile">Profile to apply to the body.</param>
    /// <param name="preset">Appearance preset to apply, if any.</param>
    /// <returns>The spawned body.</returns>
    public EntityUid SpawnAdminCharacterBody(EntityCoordinates coordinates, HumanoidCharacterProfile profile,
        HumanoidProfilePrototype? preset)
    {
        if (!_prototypeManager.TryIndex<SpeciesPrototype>(profile.Species, out var species))
            throw new ArgumentException($"Invalid species prototype was used: {profile.Species}");

        var uid = Spawn(species.Prototype, coordinates);
        ApplyAdminPresetLayers(uid, preset);

        _humanoidSystem.LoadProfile(uid, profile);
        _metaSystem.SetEntityName(uid, profile.Name);

        if (profile.FlavorText != "" && _configurationManager.GetCVar(CCVars.FlavorText))
            AddComp<DetailExaminableComponent>(uid).Content = profile.FlavorText;

        _oocNotes.SetNotes(uid, profile.OocNotes);
        ApplyProfileImage(uid, profile.ProfileImage);
        return uid;
    }

    /// <summary>
    /// Equips the standard starting gear on an admin spawned body and finishes its spawn.
    /// </summary>
    /// <param name="uid">Body to equip.</param>
    /// <param name="gear">Starting gear to equip, if any.</param>
    /// <param name="profile">Profile the loadout is taken from.</param>
    public void EquipAdminCharacterGear(EntityUid uid, StartingGearPrototype? gear, HumanoidCharacterProfile profile)
    {
        if (gear != null)
        {
            EquipAdminCharacterSpawnLoadout(uid, gear, profile, _actors.GetSession(uid));
            EquipStartingGear(uid, gear, raiseEvent: false);
        }

        var gearEquippedEv = new StartingGearEquippedEvent(uid);
        RaiseLocalEvent(uid, ref gearEquippedEv);
        _identity.QueueIdentityUpdate(uid);
    }

    private void ApplyAdminPresetLayers(EntityUid uid, HumanoidProfilePrototype? preset)
    {
        if (preset == null || !TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
            return;

        foreach (var (layer, info) in preset.CustomBaseLayers)
            appearance.CustomBaseLayers[layer] = info;
    }
}
