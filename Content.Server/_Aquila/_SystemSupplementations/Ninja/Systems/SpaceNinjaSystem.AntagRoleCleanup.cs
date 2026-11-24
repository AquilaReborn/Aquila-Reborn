using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Ninja.Systems;

public sealed partial class SpaceNinjaSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaSpaceNinjaRole = "SpaceNinja";

    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasRemoved(AquilaSpaceNinjaRole))
            return;

        if (args.HasRemainingRole(AquilaSpaceNinjaRole))
            return;

        if (args.Body is not { } body)
            return;

        RemComp<SpaceNinjaComponent>(body);
        _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);
    }
}
