using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.Roles;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ZombieRuleSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaInitialInfectedRole = "InitialInfected";
    private static readonly ProtoId<AntagPrototype> AquilaZombieRole = "Zombie";

    private static readonly ProtoId<AntagPrototype>[] AquilaZombieRoles =
    [
        AquilaInitialInfectedRole,
        AquilaZombieRole,
    ];

    private static readonly Type[] AquilaInitialInfectedComponents =
    [
        typeof(PendingZombieComponent),
        typeof(ZombifyOnDeathComponent),
        typeof(IncurableZombieComponent),
        typeof(InitialInfectedComponent),
    ];

    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasAnyRemoved(AquilaZombieRoles))
            return;

        if (args.HasAnyRemainingRole(AquilaZombieRoles))
            return;

        if (args.Body is not { } body)
            return;

        if (args.WasRemoved(AquilaInitialInfectedRole))
            _aquilaRoleCleanup.RemoveComponents(body, AquilaInitialInfectedComponents);

        _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);
    }
}
