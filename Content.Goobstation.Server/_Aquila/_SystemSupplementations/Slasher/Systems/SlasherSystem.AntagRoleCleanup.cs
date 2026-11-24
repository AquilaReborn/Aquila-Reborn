using Content.Goobstation.Shared.Slasher.Components;
using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Slasher.Systems;

public sealed partial class SlasherSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaSlasherRole = "Slasher";

    private static readonly Type[] AquilaSlasherComponents =
    [
        typeof(SlasherComponent),
        typeof(SlasherKitSelectComponent),
        typeof(SlasherSummonMacheteComponent),
        typeof(SlasherStaggerAreaComponent),
        typeof(SlasherRelentlessGrabComponent),
        typeof(SlasherRegenerateComponent),
        typeof(SlasherFearComponent),
        typeof(SlasherIncorporealComponent),
        typeof(SlasherSoulStealComponent),
        typeof(SlasherSummonMeatSpikeComponent),
        typeof(SlasherMassacreUserComponent),
        typeof(SlasherIncorporealOverlayComponent),
        typeof(SlasherRegenerateOverlayComponent),
        typeof(SlasherStaggerOverlayComponent),
    ];

    [Dependency] private AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasRemoved(AquilaSlasherRole))
            return;

        if (args.HasRemainingRole(AquilaSlasherRole))
            return;

        if (args.Body is not { } body)
            return;

        _aquilaRoleCleanup.RemoveComponents(body, AquilaSlasherComponents);
    }
}
