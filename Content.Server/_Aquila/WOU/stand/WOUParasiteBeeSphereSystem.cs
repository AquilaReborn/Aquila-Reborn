using Content.Server.Guardian;
using Content.Shared._Aquila.WOU.Stand;
using Content.Shared._Aquila.WOU.Switching;
using Content.Shared._Goobstation.Wizard.FadingTimedDespawn;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.WOU.Stand;

public sealed class WOUParasiteBeeSphereSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WOUParasiteHostComponent, WOUBeeSphereActionEvent>(OnActionFromHost);
        SubscribeLocalEvent<WOUParasiteModeComponent, WOUBeeSphereActionEvent>(OnActionFromParasite);
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
            if (TryComp<GuardianComponent>(uid, out var guardian) && guardian.Host is { } h)
                host = h;

            var entities = _lookup.GetEntitiesInRange(xform.Coordinates, sphere.Radius);
            foreach (var target in entities)
            {
                if (target == uid || target == host || !HasComp<MindContainerComponent>(target)) continue;
                if (HasComp<SwitchingComponent>(target)) continue;
                if (sphere.TriggeredEntities.Contains(target)) continue;

                sphere.TriggeredEntities.Add(target);
                SpawnBee(target, host, uid, sphere);
                sphere.BeesSpawned++;
                if (sphere.BeesSpawned >= sphere.MaxBees) break;
            }
        }
    }

    private void OnActionFromHost(EntityUid uid, WOUParasiteHostComponent comp, WOUBeeSphereActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;

        if (StartBeeSphere(parasite)) args.Handled = true;
    }

    private void OnActionFromParasite(EntityUid uid, WOUParasiteModeComponent comp, WOUBeeSphereActionEvent args)
    {
        if (args.Handled) return;

        if (StartBeeSphere(uid)) args.Handled = true;
    }

    private bool StartBeeSphere(EntityUid parasite)
    {
        if (HasComp<WOUBeeSphereComponent>(parasite)) return false;
        var sphereComp = AddComp<WOUBeeSphereComponent>(parasite);
        sphereComp.ExpiresAt = _timing.CurTime + sphereComp.Lifetime;
        return true;
    }

    private void SpawnBee(EntityUid target, EntityUid host, EntityUid parasite, WOUBeeSphereComponent sphere)
    {
        var bee = SpawnAttachedTo(sphere.BeeProto, Transform(target).Coordinates);
        var despawn = EnsureComp<FadingTimedDespawnComponent>(bee);
        despawn.Lifetime = sphere.BeeLifetime;

        if (host != default && Exists(host)) _faction.IgnoreEntity(bee, host);
        _faction.IgnoreEntity(bee, parasite);
    }
}
