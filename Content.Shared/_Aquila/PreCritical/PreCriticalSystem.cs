using Content.Shared._Aquila.CCVars;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Shared._Aquila.PreCritical;

/// <summary>
/// Держит моба в предкритическом состоянии: укладывает его, замедляет ползание и не даёт встать,
/// пока состояние активно. Переходы между живым состоянием, предкритом, критом и смертью делает
/// MobThresholdSystem по порогам прототипа, здесь только поведение.
/// </summary>
public sealed class PreCriticalSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    /// <summary>
    /// Подписывается на смену состояния моба, попытку встать и пересчёт скорости.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        if (DebugEnabled)
            Log.Info("PreCritical: system initialized");

        SubscribeLocalEvent<PreCriticalComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PreCriticalComponent, StandUpAttemptEvent>(OnStandUpAttempt);
        SubscribeLocalEvent<PreCriticalComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnMobStateChanged(Entity<PreCriticalComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.PreCritical)
            EnterPreCritical(ent);
        else if (args.OldMobState == MobState.PreCritical)
            ExitPreCritical(ent, args.NewMobState);
    }

    private void EnterPreCritical(Entity<PreCriticalComponent> ent)
    {
        if (!_net.IsServer)
            return;

        var knocked = _stun.TryKnockdown(ent.Owner, ent.Comp.HoldTime, refresh: true, autoStand: false, drop: true);

        if (!knocked)
        {
            if (!_standing.Down(ent.Owner, playSound: false, dropHeldItems: true, force: true))
            {
                if (TryComp<MobStateComponent>(ent.Owner, out var mobState))
                    _mobState.ChangeMobState(ent.Owner, MobState.Alive, mobState, origin: null);

                LogState("cannot be laid down, reverted to alive", ent.Owner);
                return;
            }

            var dropEv = new DropHandItemsEvent();
            RaiseLocalEvent(ent.Owner, ref dropEv);
        }

        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);
        LogState(knocked ? "entered pre-critical" : "entered pre-critical (forced down)", ent.Owner);
    }

    private void ExitPreCritical(Entity<PreCriticalComponent> ent, MobState newState)
    {
        if (!_net.IsServer)
            return;

        var isAlive = _mobState.IsAlive(ent.Owner);

        if (HasComp<KnockedDownComponent>(ent.Owner))
        {
            _stun.SetKnockdownTime(ent.Owner, TimeSpan.Zero);
            _stun.SetAutoStand(ent.Owner, true);

            if (isAlive)
                _stun.TryStanding(ent.Owner);
        }
        else if (isAlive && _standing.IsDown(ent.Owner))
        {
            _standing.Stand(ent.Owner, force: true);
        }

        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);

        var reason = newState switch
        {
            MobState.Critical => "went critical from damage",
            MobState.Dead => "died from damage",
            MobState.Alive => "left pre-critical by healing",
            _ => $"left pre-critical, now {newState}",
        };

        LogState(reason, ent.Owner);
    }

    private void OnStandUpAttempt(Entity<PreCriticalComponent> ent, ref StandUpAttemptEvent args)
    {
        if (!_mobState.IsPreCritical(ent.Owner))
            return;

        args.Cancelled = true;
        args.Autostand = false;
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, PreCriticalComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!_mobState.IsPreCritical(uid))
            return;

        args.ModifySpeed(component.CrawlSpeedMultiplier, true);
    }

    private bool DebugEnabled => _net.IsServer && _cfg.GetCVar(AquilaCVars.PreCriticalDebug);

    private void LogState(string action, EntityUid uid)
    {
        if (!DebugEnabled)
            return;

        if (!TryComp<DamageableComponent>(uid, out var damageable))
        {
            Log.Info($"PreCritical: {ToPrettyString(uid)} {action}");
            return;
        }

        var vital = _threshold.CheckVitalDamage(uid, damageable);
        var crit = _threshold.TryGetIncapThreshold(uid, out var threshold) ? threshold.Value.Float() : 0f;

        Log.Info($"PreCritical: {ToPrettyString(uid)} {action}, vital={vital.Float():0.0}, total={damageable.TotalDamage.Float():0.0}, crit={crit:0.0}");
    }
}
