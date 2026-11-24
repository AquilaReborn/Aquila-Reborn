using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server.Objectives.Components;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.CosmicCult;

public sealed partial class CosmicCultRuleSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaCosmicCultistRole = "CosmicAntagCultist";
    private static readonly ProtoId<AntagPrototype> AquilaCosmicColossusRole = "CosmicAntagColossus";

    private static readonly ProtoId<AntagPrototype>[] AquilaCosmicRoles =
    [
        AquilaCosmicCultistRole,
        AquilaCosmicColossusRole,
    ];

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasAnyRemoved(AquilaCosmicRoles))
            return;

        if (args.HasAnyRemainingRole(AquilaCosmicRoles))
            return;

        AquilaRemoveCosmicObjectives(args.MindId, args.Mind);

        if (args.Body is not { } body)
            return;

        if (TryComp<CosmicCultComponent>(body, out var cosmic) && cosmic.LifeStage == ComponentLifeStage.Running)
            RemComp<CosmicCultComponent>(body);

        RemComp<CosmicColossusComponent>(body);
    }

    private void AquilaRemoveCosmicObjectives(EntityUid mindId, MindComponent mind)
    {
        for (var i = mind.Objectives.Count - 1; i >= 0; i--)
        {
            var objective = mind.Objectives[i];
            if (HasComp<CosmicVictoryConditionComponent>(objective)
                || HasComp<CosmicTierConditionComponent>(objective)
                || HasComp<CosmicEntropyConditionComponent>(objective))
                _mind.TryRemoveObjective(mindId, mind, i);
        }
    }
}
