using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Client.Traits;

public sealed partial class ParacusiaSystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private void InitializeStatusSources()
    {
        SubscribeLocalEvent<ParacusiaComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<ParacusiaComponent, ComponentShutdown>(OnParacusiaShutdown);
        SubscribeLocalEvent<ParacusiaComponent, EntGotRemovedFromContainerMessage>(OnParacusiaRemovedFromContainer);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnStatusListenerDetached);
    }

    private void PlayStatusParacusiaSounds(EntityUid listener)
    {
        if (!_containers.TryGetContainer(listener, StatusEffectContainerComponent.ContainerId, out var container))
            return;

        foreach (var source in container.ContainedEntities)
        {
            if (!TryComp<ParacusiaComponent>(source, out var paracusia)
                || !TryComp<StatusEffectComponent>(source, out var status))
                continue;

            if (TerminatingOrDeleted(source)
                || status.AppliedTo != listener
                || !status.Applied
                || status.StartEffectTime > _timing.CurTime
                || status.EndEffectTime is { } end && end <= _timing.CurTime)
            {
                paracusia.Stream = _audio.Stop(paracusia.Stream);
                continue;
            }

            PlayParacusiaSounds(source, listener);
        }
    }

    private void OnStatusEffectRemoved(Entity<ParacusiaComponent> ent, ref StatusEffectRemovedEvent args)
    {
        ent.Comp.Stream = _audio.Stop(ent.Comp.Stream);
    }

    private void OnParacusiaShutdown(Entity<ParacusiaComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Stream = _audio.Stop(ent.Comp.Stream);
    }

    private void OnParacusiaRemovedFromContainer(Entity<ParacusiaComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        ent.Comp.Stream = _audio.Stop(ent.Comp.Stream);
    }

    private void OnStatusListenerDetached(LocalPlayerDetachedEvent args)
    {
        if (!_containers.TryGetContainer(args.Entity, StatusEffectContainerComponent.ContainerId, out var container))
            return;

        foreach (var source in container.ContainedEntities)
        {
            if (TryComp<ParacusiaComponent>(source, out var paracusia))
                paracusia.Stream = _audio.Stop(paracusia.Stream);
        }
    }
}
