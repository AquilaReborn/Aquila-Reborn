using Content.Goobstation.Shared.Changeling.Components;
using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.Store.Components;

namespace Content.Goobstation.Server.Changeling.GameTicking.Rules;

public sealed partial class ChangelingRuleSystem
{
    private static readonly Type[] AquilaChangelingComponents =
    [
        typeof(ChangelingComponent),
        typeof(ChangelingIdentityComponent),
        typeof(StoreComponent),
    ];

    [Dependency] private AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasRemoved(ChangelingPrototypeId))
            return;

        if (args.HasRemainingRole(ChangelingPrototypeId))
            return;

        if (args.Body is not { } body)
            return;

        if (TryComp<ChangelingIdentityComponent>(body, out var identity))
            _aquilaRoleCleanup.RemoveActions(body, identity.BaseChangelingActions);

        _aquilaRoleCleanup.RemoveComponents(body, AquilaChangelingComponents);
        _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);

        var query = EntityQueryEnumerator<ChangelingRuleComponent>();
        while (query.MoveNext(out _, out var rule))
            rule.ChangelingMinds.Remove(args.MindId);
    }
}
