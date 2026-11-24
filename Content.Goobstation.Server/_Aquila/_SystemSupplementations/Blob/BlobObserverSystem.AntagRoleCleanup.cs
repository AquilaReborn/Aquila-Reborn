using Content.Goobstation.Common.Blob;
using Content.Goobstation.Server.Blob.GameTicking;
using Content.Goobstation.Shared.Blob.Components;
using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Shared.Roles;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Blob;

public sealed partial class BlobObserverSystem
{
    private static readonly ProtoId<AntagPrototype> AquilaBlobRole = "Blob";

    private void AquilaInitializeRoleCleanup()
    {
        SubscribeLocalEvent<AntagRoleCleanupEvent>(AquilaOnAntagRoleCleanup);
    }

    private void AquilaOnAntagRoleCleanup(AntagRoleCleanupEvent args)
    {
        if (!args.WasRemoved(AquilaBlobRole))
            return;

        if (args.HasRemainingRole(AquilaBlobRole))
            return;

        var rules = EntityQueryEnumerator<BlobRuleComponent>();
        while (rules.MoveNext(out _, out var rule))
            rule.Blobs.RemoveAll(blob => blob.mindId == args.MindId);

        for (var i = args.Mind.Objectives.Count - 1; i >= 0; i--)
        {
            if (MetaData(args.Mind.Objectives[i]).EntityPrototype?.ID == BlobCaptureObjective.Id)
                _mindSystem.TryRemoveObjective(args.MindId, args.Mind, i);
        }

        if (args.Body is not { } body)
            return;

        RemComp<BlobCarrierComponent>(body);
        RemComp<StoreComponent>(body);

        if (!TryComp<BlobObserverComponent>(body, out var observer))
            return;

        if (observer.Core is { } core)
        {
            foreach (var action in core.Comp.Actions)
                _action.RemoveAction(action);

            RemComp<StoreComponent>(core.Owner);
        }

        RemComp<BlobObserverComponent>(body);
    }
}
