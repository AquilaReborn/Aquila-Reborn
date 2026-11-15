using Content.Shared._Aquila.Heretic;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Robust.Shared.Enums;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [Dependency] private readonly SharedHereticHeartWaypointerSystem _heartWaypointer = default!;

    private static readonly LocId HeartStateAliveMale = "heretic-livingheart-state-alive-male";
    private static readonly LocId HeartStateAliveFemale = "heretic-livingheart-state-alive-female";
    private static readonly LocId HeartStateAliveNeuter = "heretic-livingheart-state-alive-neuter";
    private static readonly LocId HeartStateCriticalMale = "heretic-livingheart-state-critical-male";
    private static readonly LocId HeartStateCriticalFemale = "heretic-livingheart-state-critical-female";
    private static readonly LocId HeartStateCriticalNeuter = "heretic-livingheart-state-critical-neuter";
    private static readonly LocId HeartStateDeadMale = "heretic-livingheart-state-dead-male";
    private static readonly LocId HeartStateDeadFemale = "heretic-livingheart-state-dead-female";
    private static readonly LocId HeartStateDeadNeuter = "heretic-livingheart-state-dead-neuter";

    private string GetHeartStateLoc(EntityUid target, MobState state)
    {
        var gender = TryComp<HumanoidAppearanceComponent>(target, out var humanoid) ? humanoid.Gender : Gender.Neuter;

        var locId = (state, gender) switch
        {
            (MobState.Alive, Gender.Male) => HeartStateAliveMale,
            (MobState.Alive, Gender.Female) => HeartStateAliveFemale,
            (MobState.Alive, _) => HeartStateAliveNeuter,
            (MobState.Critical, Gender.Male) => HeartStateCriticalMale,
            (MobState.Critical, Gender.Female) => HeartStateCriticalFemale,
            (MobState.Critical, _) => HeartStateCriticalNeuter,
            (MobState.Dead, Gender.Male) => HeartStateDeadMale,
            (MobState.Dead, Gender.Female) => HeartStateDeadFemale,
            _ => HeartStateDeadNeuter,
        };

        return Loc.GetString(locId);
    }
}
