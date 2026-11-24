using Content.Goobstation.Shared.Gangwars.Components;
using Content.Goobstation.Shared.Gangwars.Systems;
using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Gangwars.Systems;

public sealed partial class ServerGangwarRuleSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaGangLeaderRole = "GangLeader";
    private static readonly ProtoId<AntagPrototype> AquilaGangMemberRole = "GangMember";

    private static readonly ProtoId<AntagPrototype>[] AquilaGangRoles =
    [
        AquilaGangLeaderRole,
        AquilaGangMemberRole,
    ];

    private static readonly Type[] AquilaGangComponents =
    [
        typeof(GangLeaderComponent),
        typeof(GangMemberComponent),
        typeof(ShowGangIconsComponent),
    ];

    [Dependency] private SharedActionsSystem _aquilaActions = default!;
    [Dependency] private GangwarRuleSystem _aquilaGangwar = default!;
    [Dependency] private SharedMindSystem _aquilaMind = default!;
    [Dependency] private AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasAnyRemoved(AquilaGangRoles))
            return;

        if (args.HasAnyRemainingRole(AquilaGangRoles))
            return;

        if (args.Body is not { } body)
            return;

        if (TryComp<GangMemberComponent>(body, out var member) && member.ToggleOverlayActionEnt is { } toggle)
            _aquilaActions.RemoveAction(toggle);

        if (TryComp<GangLeaderComponent>(body, out var leader))
        {
            if (leader.ActionEnt is { } summon)
                _aquilaActions.RemoveAction(summon);

            if (leader.MemberOfferActionEnt is { } offer)
                _aquilaActions.RemoveAction(offer);

            if (leader.GangLocker is { } locker
                && TryComp<GangLockerComponent>(locker, out var lockerComp)
                && lockerComp.OwnerLeader == body)
            {
                lockerComp.OwnerLeader = null;
                Dirty(locker, lockerComp);
            }
        }

        _aquilaRoleCleanup.RemoveComponents(body, AquilaGangComponents);

        if (_aquilaMind.TryGetMind(body, out var mindId, out var mind) && mind != null)
            _aquilaGangwar.RemoveGangObjectives(mindId, mind);
    }
}
