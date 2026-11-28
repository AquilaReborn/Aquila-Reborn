using System.Numerics;
using Content.Server._Aquila.Antags.RoleCleanup.Events;
using Content.Server._Aquila.Heretic.Moon.Components;
using Content.Server.Roles;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Rotation;
using Robust.Server.GameObjects;
using Robust.Shared.Maths;
using StatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Аура вознёсшегося еретика пути Луны: периодически подтачивает рассудок всех вокруг
/// и обращает часть экипажа в лунатиков.
/// </summary>
public sealed partial class MoonAscensionSystem : SharedMoonAscensionSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly MoonSanitySystem _sanity = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoonAscensionComponent, ComponentStartup>(OnAuraStartup);
        SubscribeLocalEvent<MoonAscensionComponent, ComponentShutdown>(OnAuraShutdown);
        SubscribeLocalEvent<MoonInsanityAuraComponent, ComponentShutdown>(OnInsanityAuraShutdown);
        SubscribeLocalEvent<MoonLunaticRoleComponent, GetBriefingEvent>(OnLunaticBriefing);
        SubscribeLocalEvent<AntagRoleCleanupEvent>(OnAntagRoleCleanup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        UpdateInsanityOverlays(now);

        var query = EntityQueryEnumerator<MoonAscensionComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var aura, out var xform))
        {
            if (now < aura.NextPulse)
                continue;

            aura.NextPulse = now + aura.PulseInterval;

            var targets = _lookup.GetEntitiesInRange<MobStateComponent>(xform.Coordinates,
                aura.Radius,
                LookupFlags.Dynamic);
            foreach (var (target, _) in targets)
            {
                if (target == uid || _heretic.IsHereticOrGhoul(target) || HasComp<MoonLunaticComponent>(target))
                    continue;

                _sanity.ApplyBrainDamage(target, aura.BrainDamage);
                _status.TryUpdateStatusEffectDuration(target, MoonSanitySystem.InsanityStatus, aura.HallucinationDuration);
                RefreshInsanityOverlay(target, aura, now);
            }

            ConvertLunatics((uid, aura), uid, now, targets);
        }
    }

    private void OnAuraStartup(Entity<MoonAscensionComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.AuraVisualEntity is { } existing && !TerminatingOrDeleted(existing))
            return;

        var visual = SpawnAtPosition(ent.Comp.AuraVisual, Transform(ent).Coordinates);
        _transform.SetParent(visual, ent);
        _transform.SetLocalPosition(visual, Vector2.Zero);
        _transform.SetNoLocalRotation(visual, true);
        ent.Comp.AuraVisualEntity = visual;

        ConvertAscensionLunatics(ent);
    }

    private void OnAuraShutdown(Entity<MoonAscensionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.AuraVisualEntity is { } visual && !TerminatingOrDeleted(visual))
            QueueDel(visual);
    }

    private void OnInsanityAuraShutdown(Entity<MoonInsanityAuraComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Overlay is { } overlay && !TerminatingOrDeleted(overlay))
            QueueDel(overlay);
    }

    private void RefreshInsanityOverlay(EntityUid target, MoonAscensionComponent aura, TimeSpan now)
    {
        var affected = EnsureComp<MoonInsanityAuraComponent>(target);
        affected.EndTime = now + aura.HallucinationDuration;

        if (affected.Overlay is { } overlay && !TerminatingOrDeleted(overlay))
            return;

        var visual = SpawnAtPosition(aura.InsanityOverlay, Transform(target).Coordinates);
        SyncInsanityOverlayRotation(target, visual);
        affected.Overlay = visual;
    }

    private void UpdateInsanityOverlays(TimeSpan now)
    {
        var query = EntityQueryEnumerator<MoonInsanityAuraComponent>();
        while (query.MoveNext(out var uid, out var affected))
        {
            if (affected.Overlay is { } auraOverlay && !TerminatingOrDeleted(auraOverlay))
            {
                _transform.SetCoordinates(auraOverlay, Transform(uid).Coordinates);
                SyncInsanityOverlayRotation(uid, auraOverlay);
            }

            if (now < affected.EndTime)
                continue;

            if (affected.Overlay is { } expiredOverlay && !TerminatingOrDeleted(expiredOverlay))
                QueueDel(expiredOverlay);

            affected.Overlay = null;
            RemCompDeferred<MoonInsanityAuraComponent>(uid);
        }
    }

    private void SyncInsanityOverlayRotation(EntityUid target, EntityUid overlay)
    {
        if (!TryComp<RotationVisualsComponent>(target, out var rotationVisuals))
            return;

        var horizontal = _appearance.TryGetData<RotationState>(target, RotationVisuals.RotationState, out var state)
                         && state == RotationState.Horizontal;

        if (_appearance.TryGetData<bool>(target, BuckleVisuals.Buckled, out var buckled) && buckled)
            horizontal = true;

        Angle angle = horizontal ? rotationVisuals.HorizontalRotation : rotationVisuals.VerticalRotation;
        _transform.SetLocalRotation(overlay, angle);
    }
}
