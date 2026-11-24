using Content.Goobstation.Shared.Devil;
using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Devil.GameTicking.Rules;

public sealed partial class DevilRuleSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaDevilRole = "Devil";
    private static readonly ProtoId<AntagPrototype> AquilaThiefRole = "Thief";

    [Dependency] private AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasRemoved(AquilaDevilRole))
            return;

        if (args.HasRemainingRole(AquilaDevilRole))
            return;

        if (args.Body is not { } body)
            return;

        if (TryComp<DevilComponent>(body, out var devil))
            _aquilaRoleCleanup.RemoveActions(body, devil.BaseDevilActions);

        RemComp<DevilComponent>(body);

        if (!args.HasRemainingRole(AquilaThiefRole))
            RemComp<PacifiedComponent>(body);

        _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);
    }
}
