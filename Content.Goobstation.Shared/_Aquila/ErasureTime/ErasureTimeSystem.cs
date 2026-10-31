using Content.Goobstation.Shared.ParallaxStargaze;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Content.Shared.Actions;
using Content.Shared.Doors.Components;
using Robust.Shared.Physics.Dynamics;
using Content.Goobstation.Common.Weapons.Ranged;
using Content.Shared.Fluids;
using Content.Shared.Slippery;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Fluids.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Goobstation.Shared.Aquila.ErasureTime;

public sealed class ErasureTimeSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    private const string SlowfieldFixtureId = "erasure-time-slowfield";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ErasureTimeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ErasureTimeComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ErasureTimeComponent, ErasureTimeActionEvent>(OnErasureTimeAction);
        SubscribeLocalEvent<ErasureTimeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);

        SubscribeLocalEvent<ErasureTimeSlowedComponent, RemoveErasureTimeSlowdownEvent>(OnRemoveSlowdown);
        SubscribeLocalEvent<ErasureTimeSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedRefreshSpeed);

        SubscribeLocalEvent<ActiveErasureTimeUserComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ActiveErasureTimeUserComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ActiveErasureTimeUserComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<ActiveErasureTimeUserComponent, AmmoShotUserEvent>(OnAmmoShot);

        SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnPhysicsUpdateAfterSolve);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var cleanupQuery = EntityQueryEnumerator<ErasureTimeSlowedComponent>();
        while (cleanupQuery.MoveNext(out var target, out var slowed))
        {
            if (!slowed.IsSlowed)
                RemComp(target, slowed);
        }

        var query = EntityQueryEnumerator<ActiveErasureTimeUserComponent, ErasureTimeComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            if (!comp.Active)
                continue;

            if (_timing.CurTime < comp.EndTime)
                continue;

            EndEffect(uid, comp);
        }
    }

    private void OnStartup(EntityUid uid, ErasureTimeComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, ErasureTimeComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnErasureTimeAction(Entity<ErasureTimeComponent> ent, ref ErasureTimeActionEvent args)
    {
        if (args.Handled)
            return;

        StartEffect(ent.Owner, ent.Comp);

        args.Handled = true;
    }

    private void OnAmmoShot(Entity<ActiveErasureTimeUserComponent> ent, ref AmmoShotUserEvent args)
    {
        if (!TryComp<ErasureTimeComponent>(ent, out var comp))
            return;

        foreach (var projectile in args.FiredProjectiles)
            ApplySlowdown(ent, projectile, comp);
    }

    private void StartEffect(EntityUid uid, ErasureTimeComponent comp)
    {
        var wasActive = comp.Active;

        comp.Active = true;
        comp.EndTime = _timing.CurTime + TimeSpan.FromSeconds(comp.Duration);
        EnsureComp<ActiveErasureTimeUserComponent>(uid);

        _speed.RefreshMovementSpeedModifiers(uid);

        if (!wasActive && TryComp<EyeComponent>(uid, out var eye)) // добавить
        {
            comp.CasterOriginalDrawFov = eye.DrawFov;
            _eye.SetDrawFov(uid, false, eye);
        }

        if (!wasActive)
            CreateSlowfieldFixture(uid, comp);

        var stargaze = EnsureComp<ParallaxStargazeComponent>(uid);
        stargaze.ActivationRadius = comp.MobRadius;
        stargaze.StillTime = 0f;
        stargaze.FadeTime = comp.FadeTime;
        stargaze.MobRadius = comp.MobRadius;
        stargaze.MoveThreshold = comp.MoveThreshold;
        stargaze.Music = comp.Music;
        Dirty(uid, stargaze);

        Dirty(uid, comp);

        _audio.PlayPredicted(comp.ActivationSound, uid, uid);

        if (_netManager.IsServer) // добавить
            RaiseNetworkEvent(new ErasureTimeVisualEffectEvent(GetNetEntity(uid), comp.MobRadius, comp.WaveDuration), Filter.Pvs(uid));
    }

    private void EndEffect(EntityUid uid, ErasureTimeComponent comp)
    {
        DestroySlowfieldFixture(uid);

        var query = EntityQueryEnumerator<ErasureTimeSlowedComponent>();
        while (query.MoveNext(out var target, out var slowed))
        {
            if (slowed.Source != uid)
                continue;

            var ev = new RemoveErasureTimeSlowdownEvent(uid);
            RaiseLocalEvent(target, ref ev);
        }

        comp.Active = false;
        RemCompDeferred<ActiveErasureTimeUserComponent>(uid);
        RemCompDeferred<ParallaxStargazeComponent>(uid);

        if (TryComp<EyeComponent>(uid, out var eye)) // добавить
            _eye.SetDrawFov(uid, comp.CasterOriginalDrawFov, eye);

        _speed.RefreshMovementSpeedModifiers(uid);

        _audio.PlayPredicted(comp.DeactivationSound, uid, uid);

        if (_netManager.IsServer) // добавить
            RaiseNetworkEvent(new ErasureTimeVisualEffectEvent(GetNetEntity(uid), comp.MobRadius, comp.WaveDuration), Filter.Pvs(uid));
    }

    private void OnRefreshSpeed(Entity<ErasureTimeComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Active)
            args.ModifySpeed(ent.Comp.PlayerSpeedModifier, ent.Comp.PlayerSpeedModifier);
    }

    private static void CopyValues(ErasureTimeComponent from, ErasureTimeComponent to)
    {
        to.Duration = from.Duration;
        to.FadeTime = from.FadeTime;
        to.MobRadius = from.MobRadius;
        to.MoveThreshold = from.MoveThreshold;
        to.Music = from.Music;
        to.MobSpeedMultiplier = from.MobSpeedMultiplier;
        to.ProjectileSpeedMultiplier = from.ProjectileSpeedMultiplier;
        to.PlayerSpeedModifier = from.PlayerSpeedModifier;
        to.ActivationSound = from.ActivationSound;
        to.DeactivationSound = from.DeactivationSound;
    }

    #region Slowfield

    private void CreateSlowfieldFixture(EntityUid uid, ErasureTimeComponent comp)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        var shape = new PhysShapeCircle(comp.MobRadius);

        _fixtures.TryCreateFixture(
            uid,
            shape,
            SlowfieldFixtureId,
            collisionLayer: (int) CollisionGroup.ThrownItem,
            collisionMask: (int) (CollisionGroup.MobMask | CollisionGroup.BulletImpassable | CollisionGroup.ThrownItem),
            hard: false,
            body: physics);
    }

    private void DestroySlowfieldFixture(EntityUid uid)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        _fixtures.DestroyFixture(uid, SlowfieldFixtureId, body: physics);
    }

    private void OnStartCollide(Entity<ActiveErasureTimeUserComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<ErasureTimeComponent>(ent, out var comp))
            return;

        var target = args.OtherEntity;

        if (args.OurFixtureId != SlowfieldFixtureId || target == ent.Owner)
            return;

        ApplySlowdown(ent, target, comp);
    }

    private void OnEndCollide(Entity<ActiveErasureTimeUserComponent> ent, ref EndCollideEvent args)
    {
        var target = args.OtherEntity;

        if (!TryComp<ErasureTimeSlowedComponent>(target, out var slowed) || slowed.Source != ent.Owner)
            return;

        if (args.OurFixtureId != SlowfieldFixtureId)
            return;

        var ev = new RemoveErasureTimeSlowdownEvent(ent.Owner);
        RaiseLocalEvent(target, ref ev);
    }

    private void ApplySlowdown(EntityUid source, EntityUid target, ErasureTimeComponent comp)
    {
        if (TryComp<ErasureTimeSlowedComponent>(target, out var existing) && existing.IsSlowed)
            return;

        var slowed = EnsureComp<ErasureTimeSlowedComponent>(target);
        slowed.IsSlowed = true;
        slowed.Source = source;

        if (HasComp<MobStateComponent>(target))
        {
            slowed.SpeedMultiplier = comp.MobSpeedMultiplier;
            _speed.RefreshMovementSpeedModifiers(target);

            if (TryComp<EyeComponent>(target, out var eye))
            {
                slowed.OriginalDrawFov = eye.DrawFov;
                _eye.SetDrawFov(target, false, eye);
            }
        }
        else if (TryComp<ProjectileComponent>(target, out _))
        {
            slowed.SpeedMultiplier = comp.ProjectileSpeedMultiplier;
            ApplyProjectileSlowdown(target, slowed);
        }

        Dirty(target, slowed);
    }

    private void ApplyProjectileSlowdown(EntityUid target, ErasureTimeSlowedComponent slowed)
    {
        if (!TryComp<PhysicsComponent>(target, out var physics))
            return;

        slowed.OriginalLinearVelocity = physics.LinearVelocity;
        _physics.SetLinearVelocity(target, physics.LinearVelocity * slowed.SpeedMultiplier, body: physics);
    }

    private void OnRemoveSlowdown(Entity<ErasureTimeSlowedComponent> ent, ref RemoveErasureTimeSlowdownEvent args)
    {
        if (ent.Comp.Source != args.Source || !ent.Comp.IsSlowed)
            return;

        ent.Comp.IsSlowed = false;

        if (HasComp<MobStateComponent>(ent))
        {
            _speed.RefreshMovementSpeedModifiers(ent);

            if (TryComp<EyeComponent>(ent, out var eye))
                _eye.SetDrawFov(ent, ent.Comp.OriginalDrawFov, eye);
        }
        else if (TryComp<PhysicsComponent>(ent, out var physics)
            && ent.Comp.OriginalLinearVelocity.LengthSquared() > 0.01f)
        {
            _physics.SetLinearVelocity(ent, ent.Comp.OriginalLinearVelocity, body: physics);
        }
    }

    private void OnSlowedRefreshSpeed(Entity<ErasureTimeSlowedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (HasComp<MobStateComponent>(ent) && ent.Comp.IsSlowed)
            args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
    }

    private void OnPreventCollide(Entity<ActiveErasureTimeUserComponent> ent, ref PreventCollideEvent args)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures)
            || !fixtures.Fixtures.TryGetValue(SlowfieldFixtureId, out var slowfieldFixture)
            || args.OurFixture != slowfieldFixture)
            return;

        if (HasComp<DoorComponent>(args.OtherEntity)
            || HasComp<PuddleComponent>(args.OtherEntity)
            || HasComp<SlipperyComponent>(args.OtherEntity))
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Постоянно принудительно поддерживает замедление пуль, иначе физика сбросит скорость.
    /// </summary>
    private void OnPhysicsUpdateAfterSolve(ref PhysicsUpdateAfterSolveEvent args)
    {
        var query = EntityQueryEnumerator<ErasureTimeSlowedComponent>();
        while (query.MoveNext(out var uid, out var slowed))
        {
            if (!slowed.IsSlowed || !HasComp<ProjectileComponent>(uid) || slowed.OriginalLinearVelocity.LengthSquared() <= 0.01f)
                continue;

            var targetVelocity = slowed.OriginalLinearVelocity * slowed.SpeedMultiplier;
            if (TryComp<PhysicsComponent>(uid, out var physics)
                && (physics.LinearVelocity - targetVelocity).LengthSquared() > 0.01f)
                _physics.SetLinearVelocity(uid, targetVelocity, body: physics);
        }
    }

    #endregion
}
