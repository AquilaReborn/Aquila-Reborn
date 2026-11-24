using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Server.Antag.Components;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaRememberFactions(EntityUid body, AntagSelectionDefinition def)
    {
        foreach (var role in def.PrefRoles)
            _aquilaRoleCleanup.RememberFactions(body, role);

        if (def.MindRoles is null)
            return;

        foreach (var mindRole in def.MindRoles)
        {
            if (!ProtoMan.TryIndex(mindRole, out EntityPrototype? prototype)
                || !prototype.TryComp<MindRoleComponent>(out var role, Factory)
                || role.AntagPrototype is not { } antag)
                continue;

            _aquilaRoleCleanup.RememberFactions(body, antag);
        }
    }
}
