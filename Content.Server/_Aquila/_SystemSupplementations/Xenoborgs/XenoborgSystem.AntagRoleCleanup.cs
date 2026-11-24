using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.Roles;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoborgs;

public sealed partial class XenoborgSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaXenoborgRole = "Xenoborg";
    private static readonly ProtoId<AntagPrototype> AquilaMothershipCoreRole = "MothershipCore";

    private static readonly ProtoId<AntagPrototype>[] AquilaXenoborgRoles =
    [
        AquilaXenoborgRole,
        AquilaMothershipCoreRole,
    ];

    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasAnyRemoved(AquilaXenoborgRoles))
            return;

        if (args.HasAnyRemainingRole(AquilaXenoborgRoles))
            return;

        if (args.Body is not { } body)
            return;

        RemComp<XenoborgComponent>(body);
        RemComp<MothershipCoreComponent>(body);
        _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);
    }
}
