using System.Numerics;
using Content.Goobstation.Common.Weapons.Ranged;
using Content.Shared.Actions;
using Content.Shared.Doors.Components;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Speech;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Aquila.TimeStop;

public sealed class TimeStopSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const string FieldFixtureId = "time_stop_field";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimeStopComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TimeStopComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TimeStopComponent, TimeStopActionEvent>(OnTimeStopAction);

        SubscribeLocalEvent<TimeStopSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnSlowedRefreshSpeed);

        SubscribeLocalEvent<TimeStopSlowedComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<TimeStopSlowedComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<TimeStopSlowedComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<TimeStopSlowedComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<TimeStopSlowedComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<TimeStopSlowedComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<TimeStopSlowedComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<TimeStopSlowedComponent, EmoteAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<TimeStopSlowedComponent, SpeakAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<TimeStopSlowedComponent, ComponentInit>(OnSlowedInit);

        SubscribeLocalEvent<ActiveTimeStopComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ActiveTimeStopComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<ActiveTimeStopComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<ActiveTimeStopComponent, AmmoShotUserEvent>(OnAmmoShot);

        SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnPhysicsUpdateAfterSolve);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var cleanupQuery = EntityQueryEnumerator<TimeStopSlowedComponent>();
        while (cleanupQuery.MoveNext(out var target, out var slowed))
        {
            if (!slowed.IsSlowed && !slowed.Grayscale && _timing.CurTime >= slowed.FlashEndTime)
                RemComp(target, slowed);
        }

        if (_netManager.IsClient)
            return;

        var now = _timing.CurTime;

        var activeQuery = EntityQueryEnumerator<ActiveTimeStopComponent, TimeStopComponent, TransformComponent>();
        while (activeQuery.MoveNext(out var casterUid, out _, out var comp, out var casterXform))
        {
            if (comp.ExpiresAt is not { } expiresAt)
                continue;

            if (comp.TickSound != null && now >= comp.NextTickSoundAt)
            {
                if (comp.TickStream != null)
                    _audio.Stop(comp.TickStream);

                comp.TickStream = PlayFieldSound(casterUid, comp.TickSound, comp.Radius);
                comp.NextTickSoundAt = now + TimeSpan.FromSeconds(comp.TickInterval);
                Dirty(casterUid, comp);
            }

            var remaining = (float) (expiresAt - _timing.CurTime).TotalSeconds;
            if (remaining <= 0f)
                continue;

            var currentlyInRange = new HashSet<EntityUid> { casterUid };

            foreach (var candidate in _lookup.GetEntitiesInRange(casterXform.Coordinates, comp.Radius))
            {
                if (candidate == casterUid)
                    continue;

                if (HasComp<MobStateComponent>(candidate))
                {
                    currentlyInRange.Add(candidate);
                    continue;
                }

                if (HasComp<ThrownItemComponent>(candidate)
                    && (!TryComp<TimeStopSlowedComponent>(candidate, out var existingThrown) || !existingThrown.IsSlowed))
                {
                    ApplySlowdown(casterUid, candidate, comp, remaining);
                }
            }

            foreach (var visible in currentlyInRange)
                ApplyVisualsOnly(visible, comp);

            foreach (var previouslyVisible in comp.InRange)
            {
                if (currentlyInRange.Contains(previouslyVisible) || TerminatingOrDeleted(previouslyVisible))
                    continue;

                ReleaseVisualsOnly(previouslyVisible, comp);
            }

            comp.InRange = currentlyInRange;
        }

        var query = EntityQueryEnumerator<TimeStopComponent>();
        while (query.MoveNext(out var casterUid, out var caster))
        {
            if (caster.ExpiresAt is not { } expiresAt || now < expiresAt)
                continue;

            Disable(casterUid, caster);
        }

        var slowedQuery = EntityQueryEnumerator<TimeStopSlowedComponent>();
        while (slowedQuery.MoveNext(out var target, out var slowed))
        {
            if (!slowed.IsSlowed)
                continue;

            slowed.RemainingTime -= frameTime;

            if (slowed.RemainingTime < 0f)
                ReleaseSlowdown(target, slowed);
        }
    }

    private EntityUid? PlayFieldSound(EntityUid caster, SoundSpecifier? sound, float radius)
    {
        if (sound == null)
            return null;

        var audioParams = sound.Params
            .WithMaxDistance(radius)
            .WithRolloffFactor(0f);

        return _audio.PlayPvs(sound, caster, audioParams)?.Entity;
    }

    #region Activation

    private void OnSlowedInit(Entity<TimeStopSlowedComponent> ent, ref ComponentInit args)
    {
        var (uid, comp) = ent;

        if (!TryComp(uid, out ThrownItemComponent? thrown))
            return;

        if (!TryComp(uid, out PhysicsComponent? physics) || !TryComp(uid, out FixturesComponent? fix))
            return;

        comp.OriginalLinearVelocity = physics.LinearVelocity;
        comp.OldAngularVelocity = physics.AngularVelocity;

        _physics.SetAngularVelocity(uid, 0f, false, fix, physics);
        _physics.SetLinearVelocity(uid, Vector2.Zero, true, false, fix, physics);

        if (HasComp<CollisionWakeComponent>(uid))
        {
            comp.HadCollisionWake = true;
            RemComp<CollisionWakeComponent>(uid);
        }
        thrown.Animate = false;
        Dirty(uid, thrown);
    }

    private void OnStartup(EntityUid uid, TimeStopComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.ActionId);
    }

    private void OnShutdown(EntityUid uid, TimeStopComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnTimeStopAction(Entity<TimeStopComponent> ent, ref TimeStopActionEvent args)
    {
        if (args.Handled || _netManager.IsClient)
            return;

        if (!TryComp<PhysicsComponent>(ent.Owner, out var physics))
            return;

        args.Handled = true;

        var comp = ent.Comp;

        EnsureComp<ActiveTimeStopComponent>(ent.Owner);

        if (!TryComp<FixturesComponent>(ent.Owner, out var fixtures) ||
            !fixtures.Fixtures.ContainsKey(FieldFixtureId))
        {
            _fixtures.TryCreateFixture(
                ent.Owner,
                new PhysShapeCircle(comp.Radius),
                FieldFixtureId,
                collisionLayer: (int) CollisionGroup.ThrownItem,
                collisionMask: (int) (CollisionGroup.MobMask | CollisionGroup.BulletImpassable | CollisionGroup.ThrownItem),
                hard: false,
                body: physics);
        }

        comp.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(comp.Duration);
        comp.NextTickSoundAt = _timing.CurTime + TimeSpan.FromSeconds(comp.TickInterval);
        Dirty(ent);

        PlayFieldSound(ent.Owner, comp.StartSound, comp.Radius);

        comp.InRange.Add(ent.Owner);
        ApplyVisualsOnly(ent.Owner, comp);
    } 

    public void Disable(EntityUid uid, TimeStopComponent comp)
    {
        PlayFieldSound(uid, comp.EndSound, comp.Radius);

        if (comp.TickStream != null)
        {
            _audio.Stop(comp.TickStream);
            comp.TickStream = null;
        }

        if (TryComp<PhysicsComponent>(uid, out var physics))
            _fixtures.DestroyFixture(uid, FieldFixtureId, body: physics);

        RemCompDeferred<ActiveTimeStopComponent>(uid);

        foreach (var target in comp.InRange)
            ReleaseVisualsOnly(target, comp);
        comp.InRange.Clear();

        comp.ExpiresAt = null;
    }

    #endregion

    #region Collision

    private void OnAmmoShot(Entity<ActiveTimeStopComponent> ent, ref AmmoShotUserEvent args)
    {
        if (!TryComp<TimeStopComponent>(ent, out var comp) || comp.ExpiresAt is not { } expiresAt)
            return;

        var remaining = (float) (expiresAt - _timing.CurTime).TotalSeconds;
        if (remaining <= 0f)
            return;

        foreach (var projectile in args.FiredProjectiles)
            ApplySlowdown(ent, projectile, comp, remaining);
    }

    private void OnStartCollide(Entity<ActiveTimeStopComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<TimeStopComponent>(ent, out var comp) || comp.ExpiresAt is not { } expiresAt)
            return;

        var target = args.OtherEntity;

        if (args.OurFixtureId != FieldFixtureId || target == ent.Owner)
            return;

        var remaining = (float) (expiresAt - _timing.CurTime).TotalSeconds;
        if (remaining <= 0f)
            return;

        if (HasComp<ActiveTimeStopComponent>(target))
            return;

        if (HasComp<TimeStopImmuneComponent>(target))
        {
            return;
        }

        if (TryComp<TimeStopSlowedComponent>(target, out var existing) && existing.IsSlowed)
        {
            if (remaining <= existing.RemainingTime)
                return;

            var difference = remaining - existing.RemainingTime;

            if (TryComp<ThrownItemComponent>(target, out var existingThrown) && existingThrown.LandTime != null)
            {
                existingThrown.LandTime = existingThrown.LandTime.Value + TimeSpan.FromSeconds(difference);
                Dirty(target, existingThrown);
            }

            existing.RemainingTime = remaining;
            existing.Source = ent.Owner;
            Dirty(target, existing);
            return;
        }

        ApplySlowdown(ent.Owner, target, comp, remaining);
    }

    private void OnEndCollide(Entity<ActiveTimeStopComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != FieldFixtureId)
            return;

        var target = args.OtherEntity;

        if (!TryComp<TimeStopSlowedComponent>(target, out var slowed) || slowed.Source != ent.Owner)
            return;

        if (HasComp<ThrownItemComponent>(target))
            return;

        if (!slowed.IsSlowed)
            return;

        ReleaseSlowdown(target, slowed);
    }

    private void OnPreventCollide(Entity<ActiveTimeStopComponent> ent, ref PreventCollideEvent args)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures)
            || !fixtures.Fixtures.TryGetValue(FieldFixtureId, out var fieldFixture)
            || args.OurFixture != fieldFixture)
            return;

        if (HasComp<DoorComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void ApplySlowdown(EntityUid source, EntityUid target, TimeStopComponent comp, float remaining)
    {
        var slowed = EnsureComp<TimeStopSlowedComponent>(target);
        slowed.IsSlowed = true;
        slowed.Source = source;
        slowed.RemainingTime = remaining;

        if (HasComp<MobStateComponent>(target))
        {
            slowed.SpeedMultiplier = comp.MobSpeedMultiplier;
            _speed.RefreshMovementSpeedModifiers(target);
        }
        else if (TryComp<ProjectileComponent>(target, out _))
        {
            slowed.SpeedMultiplier = comp.ProjectileSpeedMultiplier;
            ApplyProjectileSlowdown(target, slowed);
        }
        else if (TryComp<ThrownItemComponent>(target, out var thrownItem))
        {
            if (thrownItem.LandTime != null)
            {
                thrownItem.LandTime = thrownItem.LandTime.Value + TimeSpan.FromSeconds(remaining);
                Dirty(target, thrownItem);
            }
        }
        else
        {
            RemComp<TimeStopSlowedComponent>(target);
            return;
        }

        Dirty(target, slowed);
    }

    private void ReleaseSlowdown(EntityUid uid, TimeStopSlowedComponent comp)
    {
        if (!comp.IsSlowed)
            return;

        comp.IsSlowed = false;

        if (TerminatingOrDeleted(uid))
            return;

        if (HasComp<MobStateComponent>(uid))
        {
            _speed.RefreshMovementSpeedModifiers(uid);
            Dirty(uid, comp);
            return;
        }

        if (TryComp<ThrownItemComponent>(uid, out var thrown))
        {
            if (TryComp(uid, out PhysicsComponent? physics) && TryComp(uid, out FixturesComponent? fix))
            {
                _physics.SetAngularVelocity(uid, comp.OldAngularVelocity, false, fix, physics);
                _physics.SetLinearVelocity(uid, comp.OriginalLinearVelocity, true, true, fix, physics);
            }

            if (comp.HadCollisionWake)
                EnsureComp<CollisionWakeComponent>(uid);

            thrown.Animate = true;
            Dirty(uid, thrown);
            return;
        }

        if (TryComp<PhysicsComponent>(uid, out var bulletPhysics)
            && comp.OriginalLinearVelocity.LengthSquared() > 0.01f)
            _physics.SetLinearVelocity(uid, comp.OriginalLinearVelocity, body: bulletPhysics);
    }

    private void ApplyProjectileSlowdown(EntityUid target, TimeStopSlowedComponent slowed)
    {
        if (!TryComp<PhysicsComponent>(target, out var physics))
            return;

        slowed.OriginalLinearVelocity = physics.LinearVelocity;
        _physics.SetLinearVelocity(target, physics.LinearVelocity * slowed.SpeedMultiplier, body: physics);
    }

    private void OnSlowedRefreshSpeed(Entity<TimeStopSlowedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (HasComp<MobStateComponent>(ent) && ent.Comp.IsSlowed)
            args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
    }

    #endregion

    #region Attempt blocking

    private void OnInteractAttempt(Entity<TimeStopSlowedComponent> ent, ref InteractionAttemptEvent args)
    {
        if (!ent.Comp.IsSlowed)
            return;

        args.Cancelled = true;
    }

    private void OnAttempt(EntityUid uid, TimeStopSlowedComponent component, CancellableEntityEventArgs args)
    {
        if (!component.IsSlowed)
            return;

        args.Cancel();
    }

    private void OnPullAttempt(EntityUid uid, TimeStopSlowedComponent component, PullAttemptEvent args)
    {
        if (!component.IsSlowed)
            return;

        if (args.PullerUid == uid)
            args.Cancelled = true;
    }

    private void OnPhysicsUpdateAfterSolve(ref PhysicsUpdateAfterSolveEvent args)
    {
        var query = EntityQueryEnumerator<TimeStopSlowedComponent, ThrownItemComponent>();
        while (query.MoveNext(out var uid, out var slowed, out _))
        {
            if (!slowed.IsSlowed || TerminatingOrDeleted(uid))
                continue;

            if (!TryComp(uid, out PhysicsComponent? physics) || !TryComp(uid, out FixturesComponent? fix))
                continue;

            _physics.SetAngularVelocity(uid, 0f, false, fix, physics);
            _physics.SetLinearVelocity(uid, Vector2.Zero, true, false, fix, physics);
        }
    }

    private void ApplyVisualsOnly(EntityUid target, TimeStopComponent comp)
    {
        if (TerminatingOrDeleted(target))
            return;

        var hadComponent = TryComp<TimeStopSlowedComponent>(target, out var existing);
        var slowed = EnsureComp<TimeStopSlowedComponent>(target);

        if (hadComponent && slowed.Grayscale)
            return;

        slowed.FlashDuration = comp.FlashDuration;
        slowed.FlashEndTime = _timing.CurTime + TimeSpan.FromSeconds(comp.FlashDuration);
        slowed.Grayscale = true;
        Dirty(target, slowed);
    }

    private void ReleaseVisualsOnly(EntityUid target, TimeStopComponent comp)
    {
        if (TerminatingOrDeleted(target) || !TryComp<TimeStopSlowedComponent>(target, out var slowed))
            return;

        if (!slowed.Grayscale)
            return;

        slowed.Grayscale = false;
        slowed.FlashEndTime = _timing.CurTime + TimeSpan.FromSeconds(comp.FlashDuration);
        Dirty(target, slowed);
    }

    #endregion
}
