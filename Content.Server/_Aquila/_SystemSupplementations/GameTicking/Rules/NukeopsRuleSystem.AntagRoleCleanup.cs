using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Helpers;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared.Mind.Components;
using Content.Shared.NukeOps;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class NukeopsRuleSystem
{
    private static readonly Type[] AquilaNukeopsComponents =
    [
        typeof(NukeOperativeComponent),
        typeof(SurgerySpeedModifierComponent),
    ];

    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;
    [Dependency] private readonly SharedRoleSystem _aquilaRoles = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasAnyRemoved(AntagRoleCleanupRoles.AllNukeops))
            return;

        if (args.HasAnyRemainingRole(AntagRoleCleanupRoles.AllNukeops))
            return;

        if (args.Body is not { } body)
            return;

        _aquilaRoleCleanup.RemoveComponents(body, AquilaNukeopsComponents);
        _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);
    }

    private bool AquilaHasNukeopsRole(EntityUid body)
    {
        if (!TryComp<MindContainerComponent>(body, out var container) || container.Mind is not { } mindId)
            return true;

        return _aquilaRoles.MindHasRole<NukeopsRoleComponent>(mindId);
    }
}
