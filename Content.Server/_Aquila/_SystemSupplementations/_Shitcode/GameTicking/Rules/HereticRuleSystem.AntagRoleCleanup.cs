using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Heretic;
using Content.Shared.Roles;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class HereticRuleSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaHereticRole = "Heretic";

    [Dependency] private readonly AntagRoleCleanupSystem _aquilaRoleCleanup = default!;

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasRemoved(AquilaHereticRole))
            return;

        if (args.HasRemainingRole(AquilaHereticRole))
            return;

        RemComp<HereticComponent>(args.MindId);
        RemComp<StoreComponent>(args.MindId);

        if (args.Body is { } body)
            _aquilaRoleCleanup.RestoreFactions(body, args.RemainingRoles);

        var query = EntityQueryEnumerator<HereticRuleComponent>();
        while (query.MoveNext(out _, out var rule))
            rule.Minds.Remove(args.MindId);
    }
}
