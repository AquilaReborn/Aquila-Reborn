using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Aquila.PreCritical;

/// <summary>
/// Управляет предкритом живого персонажа и его ползанием.
/// </summary>
public sealed class PreCriticalSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    /// <summary>
    /// Подписывается на попытки встать и пересчёт параметров ползания.
    /// </summary>
    public override void Initialize()
    {
        SubscribeLocalEvent<PreCriticalActiveComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<PreCriticalActiveComponent, StandUpAttemptEvent>(OnStandUpAttempt);
        SubscribeLocalEvent<PreCriticalActiveComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<PreCriticalActiveComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
        SubscribeLocalEvent<PreCriticalActiveComponent, TileFrictionEvent>(OnTileFriction);
        SubscribeLocalEvent<PreCriticalActiveComponent, DidEquipHandEvent>(OnDidEquipHand);
        SubscribeLocalEvent<PreCriticalActiveComponent, DidUnequipHandEvent>(OnDidUnequipHand);
        SubscribeLocalEvent<PreCriticalActiveComponent, HandCountChangedEvent>(OnHandCountChanged);
    }

    /// <summary>
    /// Обновляет предкрит по фактическому состоянию моба и урону жизненно важных частей тела.
    /// </summary>
    public bool UpdateEffect(Entity<PreCriticalComponent?> ent, MobState state, FixedPoint2 damage, FixedPoint2? incapThreshold)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !_net.IsServer || TerminatingOrDeleted(ent))
            return HasComp<PreCriticalActiveComponent>(ent);

        var active = state == MobState.Alive && damage >= ent.Comp.DamageThreshold &&
                     (incapThreshold is null || damage < incapThreshold.Value);

        if (HasComp<PreCriticalActiveComponent>(ent) == active)
            return active;

        if (active)
        {
            if (!_standing.Down(ent, dropHeldItems: false, force: true))
                return false;

            var effect = AddComp<PreCriticalActiveComponent>(ent);
            var drop = new DropHandItemsEvent();
            RaiseLocalEvent(ent, ref drop);
            RefreshCrawl((ent, effect));
        }
        else
        {
            ClearEffect(ent.Owner, state == MobState.Alive);
        }

        if (_cfg.GetCVar(AquilaCVars.PreCriticalDebug))
            Log.Info($"PreCritical: {ToPrettyString(ent)}, active={active}, vital={damage.Float():0.0}");

        return active;
    }

    /// <summary>
    /// Снимает предкрит и обновляет движение с учётом оставшихся эффектов.
    /// </summary>
    public void ClearEffect(Entity<PreCriticalActiveComponent?> ent, bool stand)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !_net.IsServer || TerminatingOrDeleted(ent))
            return;

        RemComp<PreCriticalActiveComponent>(ent);
        if (stand)
            _standing.Stand(ent);

        RefreshMovement(ent);
    }

    private void OnStandAttempt(Entity<PreCriticalActiveComponent> ent, ref StandAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnStandUpAttempt(Entity<PreCriticalActiveComponent> ent, ref StandUpAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnRefreshMovementSpeedModifiers(Entity<PreCriticalActiveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedModifier, true);
    }

    private void OnRefreshFrictionModifiers(Entity<PreCriticalActiveComponent> ent, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(ent.Comp.FrictionModifier);
        args.ModifyAcceleration(ent.Comp.FrictionModifier);
    }

    private void OnTileFriction(Entity<PreCriticalActiveComponent> ent, ref TileFrictionEvent args)
    {
        args.Modifier *= ent.Comp.FrictionModifier;
    }

    private void OnDidEquipHand(Entity<PreCriticalActiveComponent> ent, ref DidEquipHandEvent args)
    {
        RefreshCrawl(ent);
    }

    private void OnDidUnequipHand(Entity<PreCriticalActiveComponent> ent, ref DidUnequipHandEvent args)
    {
        RefreshCrawl(ent);
    }

    private void OnHandCountChanged(Entity<PreCriticalActiveComponent> ent, ref HandCountChangedEvent args)
    {
        RefreshCrawl(ent);
    }

    private void RefreshCrawl(Entity<PreCriticalActiveComponent> ent)
    {
        if (_timing.ApplyingState || !TryComp<PreCriticalComponent>(ent, out var settings))
            return;

        var modifiers = new KnockedDownRefreshEvent();
        RaiseLocalEvent(ent, ref modifiers);
        var speed = modifiers.SpeedModifier * settings.CrawlSpeedMultiplier;
        if (ent.Comp.SpeedModifier != speed || ent.Comp.FrictionModifier != modifiers.FrictionModifier)
        {
            ent.Comp.SpeedModifier = speed;
            ent.Comp.FrictionModifier = modifiers.FrictionModifier;
            Dirty(ent);
        }

        RefreshMovement(ent);
    }

    private void RefreshMovement(EntityUid uid)
    {
        _movement.RefreshMovementSpeedModifiers(uid);
        _movement.RefreshFrictionModifiers(uid);
    }
}
