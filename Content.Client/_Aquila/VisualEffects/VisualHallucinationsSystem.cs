using System.Diagnostics.CodeAnalysis;
using Content.Shared._Aquila.VisualEffects;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.VisualEffects;

/// <summary>
/// Планирует локальные образы без игровых сущностей и сетевых сообщений.
/// </summary>
public sealed class VisualHallucinationsSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    /// <summary>
    /// Подключает отрисовку и очистку при смене управляемого тела.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        _overlays.AddOverlay(new VisualHallucinationsOverlay());
        SubscribeLocalEvent<VisualHallucinationsStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectAppliedEvent);
        SubscribeLocalEvent<VisualHallucinationsStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemovedEvent);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttachedEvent);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetachedEvent);
    }

    /// <summary>
    /// Убирает отрисовку при завершении работы системы.
    /// </summary>
    public override void Shutdown()
    {
        _overlays.RemoveOverlay<VisualHallucinationsOverlay>();
        base.Shutdown();
    }

    /// <summary>
    /// Обновляет только локального наблюдателя и не складывает одновременно действующие эффекты.
    /// </summary>
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (_player.LocalEntity is not { } local
            || !TryComp<VisualHallucinationsComponent>(local, out var visuals))
            return;

        if (visuals.Source is not { } source || !TryGetSettings(source, local, out var settings))
        {
            RefreshSource(local, visuals);
            return;
        }

        var now = _timing.RealTime;
        if (now < visuals.NextImage)
            return;

        if (visuals.NextImage == TimeSpan.Zero)
        {
            visuals.NextImage = now + _random.Next(settings.MinInterval, settings.MaxInterval);
            return;
        }

        var xform = Transform(local);

        visuals.Image = _random.Pick(settings.Images);
        visuals.StartedAt = now;
        visuals.EndsAt = now + settings.Duration;
        visuals.NextImage = visuals.EndsAt + _random.Next(settings.MinInterval, settings.MaxInterval);
        visuals.MapId = xform.MapID;
        visuals.Position = _transform.GetWorldPosition(xform)
            + _random.NextAngle().ToVec() * _random.NextFloat(settings.MinDistance, settings.MaxDistance);
    }

    private bool TryGetSettings(EntityUid source, EntityUid local,
        [NotNullWhen(true)] out VisualHallucinationsStatusEffectComponent? settings)
    {
        return TryComp(source, out settings)
            && TryComp<StatusEffectComponent>(source, out var effect)
            && effect.AppliedTo == local
            && effect.Applied
            && effect.StartEffectTime <= _timing.CurTime
            && (effect.EndEffectTime is not { } end || end > _timing.CurTime)
            && settings.Images.Count > 0
            && settings.MinInterval > TimeSpan.Zero
            && settings.MaxInterval >= settings.MinInterval
            && settings.Duration > TimeSpan.Zero
            && settings.Duration <= TimeSpan.MaxValue - _timing.RealTime
            && settings.MaxInterval <= TimeSpan.MaxValue - _timing.RealTime - settings.Duration
            && float.IsFinite(settings.MinDistance)
            && float.IsFinite(settings.MaxDistance)
            && settings.MinDistance >= 0
            && settings.MaxDistance >= settings.MinDistance
            && float.IsFinite(settings.ImageSize)
            && settings.ImageSize > 0
            && float.IsFinite(settings.Opacity);
    }

    private void RefreshSource(EntityUid local, VisualHallucinationsComponent? visuals = null, EntityUid? excluded = null)
    {
        if (_containers.TryGetContainer(local, StatusEffectContainerComponent.ContainerId, out var container))
        {
            foreach (var source in container.ContainedEntities)
            {
                if (source == excluded || !TryGetSettings(source, local, out _))
                    continue;

                visuals ??= EnsureComp<VisualHallucinationsComponent>(local);
                if (visuals.Source != source)
                {
                    visuals.Source = source;
                    visuals.Image = null;
                }

                return;
            }
        }

        if (visuals != null)
            RemComp<VisualHallucinationsComponent>(local);
    }

    private void OnStatusEffectAppliedEvent(Entity<VisualHallucinationsStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity != args.Target || HasComp<VisualHallucinationsComponent>(args.Target))
            return;

        EnsureComp<VisualHallucinationsComponent>(args.Target).Source = ent.Owner;
    }

    private void OnStatusEffectRemovedEvent(Entity<VisualHallucinationsStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity == args.Target
            && TryComp<VisualHallucinationsComponent>(args.Target, out var visuals)
            && visuals.Source == ent.Owner)
            RefreshSource(args.Target, visuals, ent.Owner);
    }

    private void OnLocalPlayerAttachedEvent(LocalPlayerAttachedEvent args)
    {
        RefreshSource(args.Entity);
    }

    private void OnLocalPlayerDetachedEvent(LocalPlayerDetachedEvent args)
    {
        RemComp<VisualHallucinationsComponent>(args.Entity);
    }
}
