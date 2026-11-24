using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Roles;
using Content.Shared.Strip.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ThiefRuleSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaThiefRole = "Thief";
    private static readonly ProtoId<AntagPrototype> AquilaDevilRole = "Devil";

    private static readonly ProtoId<AntagPrototype>[] AquilaThiefRoles = [AquilaThiefRole];

    private static readonly Type[] AquilaThiefComponents = [typeof(ThievingComponent)];

    private static readonly Type[] AquilaThiefPacifiedComponents = [typeof(PacifiedComponent)];

    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasAnyRemoved(AquilaThiefRoles))
            return;

        if (args.HasAnyRemainingRole(AquilaThiefRoles))
            return;

        if (args.Body is not { } body)
            return;

        _aquilaRoleCleanup.RemoveComponents(body, AquilaThiefComponents);

        if (!args.HasRemainingRole(AquilaDevilRole))
            _aquilaRoleCleanup.RemoveComponents(body, AquilaThiefPacifiedComponents);

        _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);
    }
}
