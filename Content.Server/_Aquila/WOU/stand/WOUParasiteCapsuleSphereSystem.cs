using Content.Server.Explosion.EntitySystems;
using Content.Server.Guardian;
using Content.Shared._Aquila.WOU.Stand;
using Content.Shared._Aquila.WOU.Switching;
using Content.Shared.Humanoid;
using Content.Shared._Goobstation.Wizard.FadingTimedDespawn;
using Content.Shared.NPC.Systems;
using Content.Shared.Trigger;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.WOU.Stand;

public sealed class WOUParasiteCapsuleSphereSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WOUParasiteHostComponent, WOUCapsuleSphereActionEvent>(OnCapsuleFromHost);
        SubscribeLocalEvent<WOUParasiteModeComponent, WOUCapsuleSphereActionEvent>(OnCapsuleFromParasite);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = _timing.CurTime;

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
    }

    private void OnCapsuleFromHost(EntityUid uid, WOUParasiteHostComponent comp, WOUCapsuleSphereActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;
        
        if (StartCapsuleSphere(parasite)) args.Handled = true;
    }

    private void OnCapsuleFromParasite(EntityUid uid, WOUParasiteModeComponent comp, WOUCapsuleSphereActionEvent args)
    {
        if (args.Handled) return;
        
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
}