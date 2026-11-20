using Content.Server.Guardian;
using Content.Shared._Aquila.WOU.Stand;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.WOU.Stand;

public sealed class WOUParasiteDoubleReflectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WOUParasiteHostComponent, WOUDoubleReflectActionEvent>(OnDoubleReflectFromHost);
        SubscribeLocalEvent<WOUParasiteModeComponent, WOUDoubleReflectActionEvent>(OnDoubleReflectFromParasite);
        SubscribeLocalEvent<WOUParasiteDoubleReflectComponent, DamageChangedEvent>(OnDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = _timing.CurTime;

        var reflectQuery = EntityQueryEnumerator<WOUParasiteDoubleReflectComponent>();
        while (reflectQuery.MoveNext(out var uid, out var reflectComp))
        {
            if (now >= reflectComp.ExpiresAt) RemCompDeferred<WOUParasiteDoubleReflectComponent>(uid);
        }
    }

    private void OnDoubleReflectFromHost(EntityUid uid, WOUParasiteHostComponent comp, WOUDoubleReflectActionEvent args)
    {
        if (args.Handled) return;
        if (!TryComp<GuardianHostComponent>(uid, out var guardianHost) || guardianHost.HostedGuardian is not { } parasite) return;
        
        if (StartDoubleReflect(parasite)) args.Handled = true;
    }

    private void OnDoubleReflectFromParasite(EntityUid uid, WOUParasiteModeComponent comp, WOUDoubleReflectActionEvent args)
    {
        if (args.Handled) return;
        
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
}