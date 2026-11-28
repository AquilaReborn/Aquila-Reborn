using Content.Client._Aquila.Heretic.Moon.Components;
using Content.Client._Aquila.Heretic.Moon.Overlays;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Подключает локальный эффект зеркального раскола к штатным статусам.
/// </summary>
public sealed class MoonParadeFractureSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    /// <summary>
    /// Подключает обработчики жизненного цикла статуса и экранный эффект.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MoonParadeFractureStatusEffectComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<MoonParadeFractureStatusEffectComponent, ComponentShutdown>(OnComponentShutdown);
        _overlays.AddOverlay(new MoonParadeFractureOverlay(_containers));
    }

    /// <summary>
    /// Удаляет экранный эффект и освобождает экземпляр шейдера.
    /// </summary>
    public override void Shutdown()
    {
        if (_overlays.TryGetOverlay<MoonParadeFractureOverlay>(out var overlay))
        {
            _overlays.RemoveOverlay(overlay);
            overlay.Dispose();
        }
        base.Shutdown();
    }

    private void OnComponentStartup(Entity<MoonParadeFractureStatusEffectComponent> ent, ref ComponentStartup args)
    {
        if (!EnsureComp<MoonParadeFractureVisualsComponent>(ent.Owner, out var visuals))
            visuals.StartedAt = _timing.CurTime;
    }

    private void OnComponentShutdown(Entity<MoonParadeFractureStatusEffectComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Owner))
            RemComp<MoonParadeFractureVisualsComponent>(ent.Owner);
    }
}
