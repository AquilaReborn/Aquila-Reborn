using Content.Server.Guardian;
using Content.Shared._Aquila.WOU.Stand;
using Content.Shared._Aquila.WOU.Switching;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.WOU.Stand;

public sealed class WOUParasiteSlipSphereSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WOUParasiteHostComponent, WOUSlipSphereActionEvent>(OnSlipFromHost);
        SubscribeLocalEvent<WOUParasiteModeComponent, WOUSlipSphereActionEvent>(OnSlipFromParasite);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = _timing.CurTime;

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

    private void OnSlipFromHost(EntityUid uid, WOUParasiteHostComponent comp, WOUSlipSphereActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;
        
        if (StartSlipSphere(parasite)) args.Handled = true;
    }

    private void OnSlipFromParasite(EntityUid uid, WOUParasiteModeComponent comp, WOUSlipSphereActionEvent args)
    {
        if (args.Handled) return;
        
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