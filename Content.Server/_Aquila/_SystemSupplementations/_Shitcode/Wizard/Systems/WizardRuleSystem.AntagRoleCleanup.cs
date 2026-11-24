using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared._Goobstation.Wizard;
using Content.Shared._Shitcode.Wizard.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.Wizard.Systems;

public sealed partial class WizardRuleSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaWizardRole = "Wizard";
    private static readonly ProtoId<AntagPrototype> AquilaApprenticeRole = "Apprentice";
    private static readonly ProtoId<AntagPrototype> AquilaHereticRole = "Heretic";

    private static readonly ProtoId<AntagPrototype>[] AquilaWizardRoles =
    [
        AquilaWizardRole,
        AquilaApprenticeRole,
    ];

    private static readonly ProtoId<AntagPrototype>[] AquilaMindActionRoles =
    [
        AquilaWizardRole,
        AquilaApprenticeRole,
        AquilaHereticRole,
    ];

    private static readonly Type[] AquilaWizardComponents =
    [
        typeof(WizardComponent),
        typeof(ApprenticeComponent),
    ];

    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasAnyRemoved(AquilaWizardRoles))
            return;

        if (args.HasAnyRemainingRole(AquilaWizardRoles))
            return;

        if (!args.HasAnyRemainingRole(AquilaMindActionRoles))
            RemComp<ActionsContainerComponent>(args.MindId);

        if (args.Body is not { } body)
            return;

        _aquilaRoleCleanup.RemoveComponents(body, AquilaWizardComponents);
        _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);
    }

    private bool AquilaHasWizardRole(EntityUid body)
    {
        if (!TryComp<MindContainerComponent>(body, out var container) || container.Mind is not { } mindId)
            return true;

        return _role.MindHasRole<GoobWizardRoleComponent>(mindId) || _role.MindHasRole<ApprenticeRoleComponent>(mindId);
    }
}
