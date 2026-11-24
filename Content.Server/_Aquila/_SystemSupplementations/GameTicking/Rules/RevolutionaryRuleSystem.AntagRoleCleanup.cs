using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Server.Revolutionary.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class RevolutionaryRuleSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaHeadRevRole = "HeadRev";
    private static readonly ProtoId<AntagPrototype> AquilaRevRole = "Rev";

    private static readonly ProtoId<AntagPrototype>[] AquilaRevolutionaryRoles =
    [
        AquilaHeadRevRole,
        AquilaRevRole,
    ];

    private static readonly Type[] AquilaRevolutionaryComponents = [typeof(RevolutionaryComponent)];

    private static readonly Type[] AquilaHeadRevolutionaryComponents = [typeof(HeadRevolutionaryComponent)];

    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaRememberConversionFactions(EntityUid target)
    {
        _aquilaRoleCleanup.RememberFactions(target, AquilaRevRole);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasAnyRemoved(AquilaRevolutionaryRoles))
            return;

        if (args.HasAnyRemainingRole(AquilaRevolutionaryRoles))
            return;

        if (args.Body is not { } body)
            return;

        _aquilaRoleCleanup.RemoveComponents(body, AquilaRevolutionaryComponents);
        _aquilaRoleCleanup.RemoveComponents(body, AquilaHeadRevolutionaryComponents);

        if (TryComp<CommandStaffComponent>(body, out var command))
            command.Enabled = true;

        _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);
    }
}
