using Content.Server.Aquila.LifeCreator;
using Content.Server.Guardian;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Aquila.WOU.RemoteHoloParasite;
using Content.Shared.Aquila.WOU.Stand;
using Content.Shared.Aquila.WOU.Switching;
using Content.Shared.Flash;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Reflect;
using Content.Shared.NPC.Systems;
using Content.Shared.Mind.Components;
using Content.Shared._Goobstation.Wizard.FadingTimedDespawn;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Trigger;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Aquila.WOU.Stand;

public sealed class WOUParasiteAbilitiesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WOUParasiteHostComponent, WOUBeeSphereActionEvent>(OnActionFromHost);
        SubscribeLocalEvent<WOUParasiteModeComponent, WOUBeeSphereActionEvent>(OnActionFromParasite);
        SubscribeLocalEvent<WOUParasiteHostComponent, WOUDoubleReflectActionEvent>(OnDoubleReflectFromHost);
        SubscribeLocalEvent<WOUParasiteModeComponent, WOUDoubleReflectActionEvent>(OnDoubleReflectFromParasite);
        SubscribeLocalEvent<WOUParasiteDoubleReflectComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<WOUParasiteHostComponent, WOUCapsuleSphereActionEvent>(OnCapsuleFromHost);
        SubscribeLocalEvent<WOUParasiteModeComponent, WOUCapsuleSphereActionEvent>(OnCapsuleFromParasite);
        SubscribeLocalEvent<WOUParasiteHostComponent, WOUSlipSphereActionEvent>(OnSlipFromHost);
        SubscribeLocalEvent<WOUParasiteModeComponent, WOUSlipSphereActionEvent>(OnSlipFromParasite);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = _timing.CurTime;

        var beeQuery = EntityQueryEnumerator<WOUBeeSphereComponent, TransformComponent>();
        while (beeQuery.MoveNext(out var uid, out var sphere, out var xform))
        {
            if (now >= sphere.ExpiresAt || sphere.BeesSpawned >= sphere.MaxBees)
            {
                RemCompDeferred<WOUBeeSphereComponent>(uid);
                continue;
            }
            EntityUid host = default;
            if (TryComp<GuardianComponent>(uid, out var guardian) && guardian.Host is { } h) host = h;
            var entities = _lookup.GetEntitiesInRange(xform.Coordinates, sphere.Radius);
            foreach (var target in entities)
            {
                if (target == uid || target == host || !HasComp<MindContainerComponent>(target)) continue;
                if (HasComp<SwitchingComponent>(target)) continue;
                if (sphere.TriggeredEntities.Contains(target)) continue;
                sphere.TriggeredEntities.Add(target);
                SpawnBee(target, host, uid, sphere.BeeProto);
                sphere.BeesSpawned++;
                if (sphere.BeesSpawned >= sphere.MaxBees) break;
            }
        }

        var reflectQuery = EntityQueryEnumerator<WOUParasiteDoubleReflectComponent>();
        while (reflectQuery.MoveNext(out var uid, out var reflectComp))
        {
            if (now >= reflectComp.ExpiresAt) RemCompDeferred<WOUParasiteDoubleReflectComponent>(uid);
        }

        var capsuleQuery = EntityQueryEnumerator<WOUCapsuleSphereComponent, TransformComponent>();
        while (capsuleQuery.MoveNext(out var uid, out var sphere, out var xform))
        {
            if (now >= sphere.ExpiresAt || sphere.CapsulesDropped >= sphere.MaxCapsules)
            {
                RemCompDeferred<WOUCapsuleSphereComponent>(uid);
                continue;
            }
            EntityUid host = default;
            if (TryComp<GuardianComponent>(uid, out var guardian) && guardian.Host is { } h) host = h;
            var entities = _lookup.GetEntitiesInRange(xform.Coordinates, sphere.Radius);
            foreach (var target in entities)
            {
                if (target == uid || target == host) continue;
                if (HasComp<SwitchingComponent>(target)) continue;
                if (!HasComp<HumanoidAppearanceComponent>(target)) continue;
                if (sphere.TriggeredEntities.Contains(target)) continue;
                sphere.TriggeredEntities.Add(target);
                SpawnCapsule(target, uid, sphere.CapsuleProto);
                sphere.CapsulesDropped++;
                if (sphere.CapsulesDropped >= sphere.MaxCapsules) break;
            }
        }

        var slipQuery = EntityQueryEnumerator<WOUParasiteSlipSphereComponent, TransformComponent>();
        while (slipQuery.MoveNext(out var uid, out var slipSphere, out var xform))
        {
            if (now >= slipSphere.ExpiresAt)
            {
                RemCompDeferred<WOUParasiteSlipSphereComponent>(uid);
                continue;
            }
            EntityUid host = default;
            if (TryComp<GuardianComponent>(uid, out var guardian) && guardian.Host is { } h) host = h;
            var entities = _lookup.GetEntitiesInRange(xform.Coordinates, slipSphere.Radius);
            foreach (var target in entities)
            {
                if (target == uid || target == host) continue;
                if (HasComp<SwitchingComponent>(target)) continue;
                if (!HasComp<MindContainerComponent>(target)) continue;
                if (slipSphere.TriggeredEntities.Contains(target)) continue;
                if (!TryComp<PhysicsComponent>(target, out var physics) || physics.LinearVelocity.LengthSquared() < 0.01f) continue;
                _stun.TryKnockdown(target, TimeSpan.FromSeconds(3), true);
                var damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), 40f);
                _damageable.TryChangeDamage(target, damage, canMiss: true, origin: uid);
                _stamina.TakeStaminaDamage(target, 200f);
                slipSphere.TriggeredEntities.Add(target);
            }
        }
    }

    private void OnActionFromHost(EntityUid uid, WOUParasiteHostComponent comp, WOUBeeSphereActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;
        if (!TryComp<WOUParasiteModeComponent>(parasite, out var modeComp) || !modeComp.IsModeTwo) return;
        if (StartBeeSphere(parasite)) args.Handled = true;
    }

    private void OnActionFromParasite(EntityUid uid, WOUParasiteModeComponent comp, WOUBeeSphereActionEvent args)
    {
        if (args.Handled) return;
        if (!comp.IsModeTwo) return;
        if (StartBeeSphere(uid)) args.Handled = true;
    }

    private bool StartBeeSphere(EntityUid parasite)
    {
        if (HasComp<WOUBeeSphereComponent>(parasite)) return false;
        var sphereComp = AddComp<WOUBeeSphereComponent>(parasite);
        sphereComp.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(5);
        return true;
    }

    private void SpawnBee(EntityUid target, EntityUid host, EntityUid parasite, string proto)
    {
        var bee = Spawn(proto, Transform(target).Coordinates);
        var despawn = EnsureComp<FadingTimedDespawnComponent>(bee);
        despawn.Lifetime = 10f;
        if (host != default && Exists(host)) _faction.IgnoreEntity(bee, host);
        _faction.IgnoreEntity(bee, parasite);
    }

    private void OnDoubleReflectFromHost(EntityUid uid, WOUParasiteHostComponent comp, WOUDoubleReflectActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;
        if (!TryComp<WOUParasiteModeComponent>(parasite, out var modeComp) || !modeComp.IsModeTwo) return;
        if (StartDoubleReflect(parasite)) args.Handled = true;
    }

    private void OnDoubleReflectFromParasite(EntityUid uid, WOUParasiteModeComponent comp, WOUDoubleReflectActionEvent args)
    {
        if (args.Handled) return;
        if (!comp.IsModeTwo) return;
        if (StartDoubleReflect(uid)) args.Handled = true;
    }

    private bool StartDoubleReflect(EntityUid parasite)
    {
        if (HasComp<WOUParasiteDoubleReflectComponent>(parasite)) return false;
        var reflectComp = AddComp<WOUParasiteDoubleReflectComponent>(parasite);
        reflectComp.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(5);
        return true;
    }

    private void OnDamageChanged(EntityUid uid, WOUParasiteDoubleReflectComponent comp, DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() <= 0) return;
        if (args.Origin is not { } origin || origin == uid || !Exists(origin)) return;
        if (HasComp<WOUParasiteDoubleReflectComponent>(origin)) return;
        var reflectDamage = args.DamageDelta * 2;
        _damageable.TryChangeDamage(origin, reflectDamage, ignoreResistances: true, origin: uid);
    }

    private void OnCapsuleFromHost(EntityUid uid, WOUParasiteHostComponent comp, WOUCapsuleSphereActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;
        if (!TryComp<WOUParasiteModeComponent>(parasite, out var modeComp) || !modeComp.IsModeTwo) return;
        if (StartCapsuleSphere(parasite)) args.Handled = true;
    }

    private void OnCapsuleFromParasite(EntityUid uid, WOUParasiteModeComponent comp, WOUCapsuleSphereActionEvent args)
    {
        if (args.Handled) return;
        if (!comp.IsModeTwo) return;
        if (StartCapsuleSphere(uid)) args.Handled = true;
    }

    private bool StartCapsuleSphere(EntityUid parasite)
    {
        if (HasComp<WOUCapsuleSphereComponent>(parasite)) return false;
        var sphereComp = AddComp<WOUCapsuleSphereComponent>(parasite);
        sphereComp.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(4);
        return true;
    }

    private void SpawnCapsule(EntityUid target, EntityUid parasite, string proto)
    {
        var capsule = Spawn(proto, Transform(target).Coordinates);
        Timer.Spawn(TimeSpan.FromSeconds(1.5), () =>
        {
            if (!Exists(capsule)) return;
            var coords = Transform(capsule).Coordinates;
            var triggerEv = new TriggerEvent(capsule, null);
            RaiseLocalEvent(capsule, ref triggerEv);
            _explosion.QueueExplosion(_transform.GetMapCoordinates(capsule), "Default", totalIntensity: 50f, slope: 1.5f, maxTileIntensity: 1f, cause: null, tileBreakScale: 0f, maxTileBreak: 0, canCreateVacuum: false);

            Timer.Spawn(TimeSpan.FromSeconds(1.0), () =>
            {
                if (!Exists(parasite)) return;
                EntityUid host = default;
                if (TryComp<GuardianComponent>(parasite, out var guardian) && guardian.Host is { } h) host = h;
                for (var i = 0; i < 2; i++)
                {
                    var rat = Spawn("WOUMobRatAggressive", coords);
                    var despawn = EnsureComp<FadingTimedDespawnComponent>(rat);
                    despawn.Lifetime = 10f;
                    if (host != default && Exists(host)) _faction.IgnoreEntity(rat, host);
                    _faction.IgnoreEntity(rat, parasite);
                }
            });
        });
    }

    private void OnSlipFromHost(EntityUid uid, WOUParasiteHostComponent comp, WOUSlipSphereActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;
        if (!TryComp<WOUParasiteModeComponent>(parasite, out var modeComp) || !modeComp.IsModeTwo) return;
        if (StartSlipSphere(parasite)) args.Handled = true;
    }

    private void OnSlipFromParasite(EntityUid uid, WOUParasiteModeComponent comp, WOUSlipSphereActionEvent args)
    {
        if (args.Handled) return;
        if (!comp.IsModeTwo) return;
        if (StartSlipSphere(uid)) args.Handled = true;
    }

    private bool StartSlipSphere(EntityUid parasite)
    {
        if (HasComp<WOUParasiteSlipSphereComponent>(parasite)) return false;
        var sphereComp = AddComp<WOUParasiteSlipSphereComponent>(parasite);
        sphereComp.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(3);
        return true;
    }
}

public sealed class WOUParasiteModeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WOUParasiteModeComponent, ComponentStartup>(OnModeStartup);
        SubscribeLocalEvent<WOUParasiteModeComponent, ComponentShutdown>(OnModeShutdown);
        SubscribeLocalEvent<WOUParasiteModeComponent, WOUToggleFormActionEvent>(OnToggleFormSelf);
        SubscribeLocalEvent<WOUParasiteHostComponent, ComponentStartup>(OnWOUHostStartup);
        SubscribeLocalEvent<WOUParasiteHostComponent, ComponentShutdown>(OnWOUHostShutdown);
        SubscribeLocalEvent<WOUParasiteHostComponent, WOUToggleFormActionEvent>(OnToggleFormRemote);
        SubscribeLocalEvent<WOUParasiteHostComponent, SoulSwapActionEvent>(OnHostSoulSwap);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<WOUParasiteModeComponent, GuardianComponent>();
        while (query.MoveNext(out var parasite, out _, out var guardian))
        {
            if (guardian.Host is not { } host || !Exists(host)) continue;
            if (!HasComp<RemoteHoloParasiteHostComponent>(host)) continue;
            if (!HasComp<WOUParasiteHostComponent>(host)) EnsureComp<WOUParasiteHostComponent>(host);
        }
    }

    private void OnModeStartup(EntityUid uid, WOUParasiteModeComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.BeeSphereActionEntity, comp.BeeSphereAction);
        _actions.AddAction(uid, ref comp.DoubleReflectActionEntity, comp.DoubleReflectAction);
        _actions.AddAction(uid, ref comp.CapsuleSphereActionEntity, comp.CapsuleSphereAction);
        _actions.AddAction(uid, ref comp.SlipSphereActionEntity, comp.SlipSphereAction);
        ApplyMode(uid, comp, comp.StartInModeTwo, playEffects: false);
    }

    private void OnModeShutdown(EntityUid uid, WOUParasiteModeComponent comp, ComponentShutdown args)
    {
        if (comp.BeeSphereActionEntity is { } beeAct) _actions.RemoveAction(uid, beeAct);
        if (comp.DoubleReflectActionEntity is { } drAct) _actions.RemoveAction(uid, drAct);
        if (comp.CapsuleSphereActionEntity is { } csAct) _actions.RemoveAction(uid, csAct);
        if (comp.SlipSphereActionEntity is { } ssAct) _actions.RemoveAction(uid, ssAct);
        if (!Exists(uid)) return;
        RemComp<ReflectComponent>(uid);
        RemComp<ReflectMeleeDamageComponent>(uid);
        RemComp<ItemToggleComponent>(uid);
    }

    private void OnWOUHostStartup(EntityUid uid, WOUParasiteHostComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.BeeSphereActionEntity, comp.BeeSphereAction);
        _actions.AddAction(uid, ref comp.DoubleReflectActionEntity, comp.DoubleReflectAction);
        _actions.AddAction(uid, ref comp.CapsuleSphereActionEntity, comp.CapsuleSphereAction);
        _actions.AddAction(uid, ref comp.SlipSphereActionEntity, comp.SlipSphereAction);
    }

    private void OnWOUHostShutdown(EntityUid uid, WOUParasiteHostComponent comp, ComponentShutdown args)
    {
        if (comp.BeeSphereActionEntity is { } a2) _actions.RemoveAction(uid, a2);
        if (comp.DoubleReflectActionEntity is { } a3) _actions.RemoveAction(uid, a3);
        if (comp.CapsuleSphereActionEntity is { } a4) _actions.RemoveAction(uid, a4);
        if (comp.SlipSphereActionEntity is { } a5) _actions.RemoveAction(uid, a5);
    }

    private void OnToggleFormSelf(EntityUid uid, WOUParasiteModeComponent comp, WOUToggleFormActionEvent args)
    {
        if (args.Handled) return;
        ApplyMode(uid, comp, !comp.IsModeTwo);
        args.Handled = true;
    }

    private void OnToggleFormRemote(EntityUid uid, WOUParasiteHostComponent comp, WOUToggleFormActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;
        if (!TryComp<WOUParasiteModeComponent>(parasite, out var modeComp)) return;
        ApplyMode(parasite, modeComp, !modeComp.IsModeTwo);
        args.Handled = true;
    }

    private void OnHostSoulSwap(EntityUid uid, WOUParasiteHostComponent comp, SoulSwapActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;
        if (!TryComp<WOUParasiteModeComponent>(parasite, out var modeComp)) return;
        if (!modeComp.IsModeTwo) ApplyMode(parasite, modeComp, true);
    }

    public void ApplyMode(EntityUid uid, WOUParasiteModeComponent comp, bool modeTwo, bool playEffects = true)
    {
        comp.IsModeTwo = modeTwo;
        _metaData.SetEntityName(uid, modeTwo ? comp.ModeTwoName : comp.ModeOneName);
        var appearanceState = modeTwo ? WOUVisualState.ModeTwo : WOUVisualState.ModeOne;
        _appearance.SetData(uid, WOUVisuals.State, appearanceState);
        var walk = modeTwo ? comp.ModeTwoWalkSpeed : comp.ModeOneWalkSpeed;
        var sprint = modeTwo ? comp.ModeTwoSprintSpeed : comp.ModeOneSprintSpeed;
        _speed.ChangeBaseSpeed(uid, walk, sprint, 20f);

        if (modeTwo)
        {
            var reflect = EnsureComp<ReflectComponent>(uid);
            reflect.ReflectProb = 1f;
            reflect.Spread = Angle.Zero;
            reflect.Reflects = ReflectType.Energy | ReflectType.NonEnergy | ReflectType.Magic;
            reflect.InRightPlace = true;
            Dirty(uid, reflect);
            var toggle = EnsureComp<ItemToggleComponent>(uid);
            toggle.Activated = true;
            Dirty(uid, toggle);
            EnsureComp<ReflectMeleeDamageComponent>(uid);
        }
        else
        {
            RemComp<ReflectComponent>(uid);
            RemComp<ReflectMeleeDamageComponent>(uid);
            RemComp<ItemToggleComponent>(uid);
        }

        if (playEffects)
        {
            _flash.FlashArea(uid, null, comp.FlashRange, comp.FlashDuration);
            _audio.PlayPvs(comp.SwitchSound, uid);
        }
    }
}