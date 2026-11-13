using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Aquila.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IServerPreferencesManager _spawnPreferences = default!;
    [Dependency] private readonly IConfigurationManager _spawnConfiguration = default!;
    [Dependency] private readonly IAdminLogManager _spawnAdminLog = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;

    private static readonly LocId SpawnCoordinatesFailed = "admin-player-spawn-failed";
    private static readonly LocId SpawnHere = "admin-player-actions-spawn";
    private static readonly LocId CloneHere = "admin-player-actions-clone";

    private void AddAdminCharacterSpawnVerbs(GetVerbsEvent<Verb> args, ICommonSession player, ICommonSession targetPlayer)
    {
        if (!CanCustomizeSpawn(args.Target, player, targetPlayer))
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(SpawnHere),
            Category = VerbCategory.Admin,
            Act = () => OpenAdminCharacterSpawn(args.Target, player, targetPlayer, clone: false),
            Impact = LogImpact.High,
        });
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(CloneHere),
            Category = VerbCategory.Admin,
            Act = () => OpenAdminCharacterSpawn(args.Target, player, targetPlayer, clone: true),
            Impact = LogImpact.High,
        });
    }

    private bool CanCustomizeSpawn(EntityUid target, ICommonSession player, ICommonSession targetPlayer)
    {
        return _adminManager.HasAdminFlag(player, AdminFlags.Spawn)
            && !TerminatingOrDeleted(player.AttachedEntity)
            && !TerminatingOrDeleted(target)
            && targetPlayer.AttachedEntity == target
            && TryComp<ActorComponent>(target, out var actor)
            && actor.PlayerSession == targetPlayer;
    }

    private void OpenAdminCharacterSpawn(EntityUid target, ICommonSession player, ICommonSession targetPlayer, bool clone)
    {
        if (!CanCustomizeSpawn(target, player, targetPlayer)
            || !_spawnPreferences.TryGetCachedPreferences(targetPlayer.UserId, out var targetPreferences)
            || !targetPreferences.Characters.TryGetValue(targetPreferences.SelectedCharacterIndex, out var selected)
            || selected is not HumanoidCharacterProfile targetProfile)
        {
            return;
        }

        TryGetAdminSpawnStorage(player, out var storage);
        var state = new AdminCharacterSpawnState
        {
            TargetProfile = CreateSpawnPreview(targetProfile),
            Clone = clone,
            CanSpawnInsideStorage = storage.HasValue,
        };

        if (_spawnPreferences.TryGetCachedPreferences(player.UserId, out var preferences))
        {
            foreach (var (slot, character) in preferences.Characters)
            {
                if (character is HumanoidCharacterProfile profile)
                    state.Profiles.Add(slot, CreateSpawnPreview(profile));
            }
        }

        if (player.UserId != targetPlayer.UserId)
        {
            foreach (var (slot, character) in targetPreferences.Characters)
            {
                if (character is HumanoidCharacterProfile profile)
                    state.TargetProfiles.Add(slot, CreateSpawnPreview(profile));
            }
        }

        _euiManager.OpenEui(new AdminCharacterSpawnEui(this, target, targetPlayer, state, storage, preferences, targetPreferences), player);
    }

    private static HumanoidCharacterProfile CreateSpawnPreview(HumanoidCharacterProfile profile)
    {
        return new HumanoidCharacterProfile
        {
            Name = profile.Name,
            Species = profile.Species,
            Appearance = profile.Appearance,
            Age = profile.Age,
            Height = profile.Height,
            Width = profile.Width,
        }.WithSex(profile.Sex).WithGender(profile.Gender);
    }

    internal bool TrySpawnAdminCharacter(EntityUid target, ICommonSession player, ICommonSession targetPlayer,
        bool clone, AdminCharacterSpawnRequest request, EntityUid? offeredStorage, PlayerPreferences? preferences, PlayerPreferences targetPreferences)
    {
        if (!CanCustomizeSpawn(target, player, targetPlayer) || player.AttachedEntity is not { } user)
            return false;

        EntityUid mind = default;
        MindComponent? mindComp = null;
        if (!clone && (!_mindSystem.TryGetMind(target, out mind, out mindComp)
            || TerminatingOrDeleted(mind)
            || mindComp.UserId != targetPlayer.UserId))
            return false;

        EntityUid? storage = null;
        if (request.SpawnInsideStorage
            && (!TryGetAdminSpawnStorage(player, out storage) || storage != offeredStorage))
            return false;

        var destination = storage ?? (clone ? user : target);
        if (!_transformSystem.TryGetMapOrGridCoordinates(destination, out var coordinates))
        {
            _popup.PopupEntity(Loc.GetString(SpawnCoordinatesFailed), user, user);
            return false;
        }

        if (!TryResolveSpawnProfile(preferences, targetPreferences, request, out var profile, out var preset)
            || !_prototypeManager.TryIndex(profile.Species, out _))
        {
            return false;
        }

        StartingGearPrototype? gear = null;
        if (request.Gear is { } gearId
            && (!_prototypeManager.TryIndex(gearId, out gear) || gear.Abstract))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            if (name.Length > _spawnConfiguration.GetCVar(CCVars.MaxNameLength) || name.Any(char.IsControl))
                return false;

            profile = profile.WithName(name);
        }

        var station = _stations.GetOwningStation(target);
        var mob = _spawning.SpawnPlayerMob(coordinates.Value, null, profile, station,
            adminGear: gear, adminPreset: preset);

        if (storage != null && !TryPlaceAdminSpawnInsideStorage(mob, storage))
        {
            Del(mob);
            return false;
        }

        if (mindComp != null)
            _mindSystem.TransferTo(mind, mob, true, mind: mindComp);

        var action = clone ? "cloned" : "respawned";
        _spawnAdminLog.Add(LogType.AdminCommands, LogImpact.High,
            $"{ToPrettyString(user):user} {action} {ToPrettyString(target):target} as {ToPrettyString(mob):spawned}");
        return true;
    }

    private bool TryResolveSpawnProfile(PlayerPreferences? preferences, PlayerPreferences targetPreferences, AdminCharacterSpawnRequest request,
        [NotNullWhen(true)] out HumanoidCharacterProfile? profile, out HumanoidProfilePrototype? preset)
    {
        profile = null;
        preset = null;

        if (request.Slot is { } slot)
        {
            var source = request.UseTargetSlot ? targetPreferences : preferences;
            if (request.Preset != null || source == null || !source.Characters.TryGetValue(slot, out var character))
            {
                return false;
            }

            profile = character as HumanoidCharacterProfile;
            return profile != null;
        }

        if (request.Preset is { } presetId)
        {
            if (!_prototypeManager.TryIndex(presetId, out preset))
                return false;

            profile = preset.Profile;
            return true;
        }

        if (!targetPreferences.Characters.TryGetValue(targetPreferences.SelectedCharacterIndex, out var selected))
        {
            return false;
        }

        profile = selected as HumanoidCharacterProfile;
        return profile != null;
    }
}
