using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Helpers;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.RoleCodeword;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class TraitorRuleSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaTraitorRole = "Traitor";
    private static readonly ProtoId<AntagPrototype> AquilaTraitorSleeperRole = "TraitorSleeper";

    private static readonly ProtoId<AntagPrototype>[] AquilaTraitorRoles =
    [
        AquilaTraitorRole,
        AquilaTraitorSleeperRole,
    ];

    private static readonly ProtoId<AntagPrototype>[] AquilaUplinkRoles =
    [
        AquilaTraitorRole,
        AquilaTraitorSleeperRole,
        ..AntagRoleCleanupRoles.AllNukeops,
    ];

    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasAnyRemoved(AquilaTraitorRoles))
            return;

        if (args.HasAnyRemainingRole(AquilaTraitorRoles))
            return;

        RemComp<RoleCodewordComponent>(args.MindId);

        if (args.Body is { } body)
        {
            if (!args.HasAnyRemainingRole(AquilaUplinkRoles))
                _aquilaRoleCleanup.RemoveUplink(body);

            _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);
        }

        var query = EntityQueryEnumerator<TraitorRuleComponent>();
        while (query.MoveNext(out _, out var rule))
            rule.TraitorMinds.Remove(args.MindId);
    }
}
