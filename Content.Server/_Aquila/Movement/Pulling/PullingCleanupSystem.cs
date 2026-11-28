using Content.Shared.Movement.Pulling.Components;

namespace Content.Server._Aquila.Movement.Pulling;

public sealed class PullingCleanupSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PullableComponent, EntityTerminatingEvent>(OnPullableTerminating);
        SubscribeLocalEvent<PullerComponent, EntityTerminatingEvent>(OnPullerTerminating);
    }

    private void OnPullableTerminating(Entity<PullableComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Puller is not { } puller || !TryComp<PullerComponent>(puller, out var pullerComp))
            return;

        pullerComp.Pulling = null;
        Dirty(puller, pullerComp);
        RemComp<ActivePullerComponent>(puller);
    }

    private void OnPullerTerminating(Entity<PullerComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Pulling is not { } pullable || !TryComp<PullableComponent>(pullable, out var pullableComp))
            return;

        pullableComp.Puller = null;
        pullableComp.PullJointId = null;
        Dirty(pullable, pullableComp);
    }
}
