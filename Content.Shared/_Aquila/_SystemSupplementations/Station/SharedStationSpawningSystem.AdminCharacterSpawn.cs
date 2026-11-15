using Content.Shared.Clothing;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Player;

namespace Content.Shared.Station;

public abstract partial class SharedStationSpawningSystem
{
    /// <summary>
    /// Надевает лоадаут должности, которой принадлежит указанное стартовое снаряжение.
    /// </summary>
    /// <param name="uid">Сущность, на которую надевается лоадаут.</param>
    /// <param name="gear">Стартовое снаряжение, по которому определяется должность.</param>
    /// <param name="profile">Профиль персонажа, из которого берётся лоадаут.</param>
    /// <param name="session">Сессия игрока, если она есть.</param>
    public void EquipAdminCharacterSpawnLoadout(EntityUid uid, StartingGearPrototype gear, HumanoidCharacterProfile? profile, ICommonSession? session)
    {
        if (profile == null)
            return;

        if (FindAdminCharacterSpawnJob(gear.ID) is not { } job)
            return;

        var loadoutId = LoadoutSystem.GetJobPrototype(job.ID);
        if (!PrototypeManager.TryIndex(loadoutId, out RoleLoadoutPrototype? loadoutPrototype))
            return;

        RoleLoadout? loadout = null;
        profile.Loadouts.TryGetValue(loadoutId, out loadout);

        if (loadout == null)
        {
            loadout = new RoleLoadout(loadoutId);
            loadout.SetDefault(profile, session, PrototypeManager);
        }

        var name = Name(uid);
        EquipRoleLoadout(uid, loadout, loadoutPrototype, equipPersonal: false);
        _metadata.SetEntityName(uid, name);
    }

    private JobPrototype? FindAdminCharacterSpawnJob(string gearId)
    {
        JobPrototype? job = null;

        foreach (var candidate in PrototypeManager.EnumeratePrototypes<JobPrototype>())
        {
            if (candidate.StartingGear is not { } candidateGear || candidateGear.Id != gearId)
                continue;

            if (job == null || string.CompareOrdinal(candidate.ID, job.ID) < 0)
                job = candidate;
        }

        return job;
    }
}
