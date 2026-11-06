using Content.Shared._Aquila.Ladder.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Aquila.Ladder;

/// <summary>
/// Лестницы и люки: кто встал на лестницу и простоял рядом нужное время, переносится к связанной лестнице.
/// Здесь начало ожидания и проверки, сам перенос делает серверная система: выход может быть вне PVS клиента.
/// </summary>
public abstract class SharedLadderSystem : EntitySystem
{
    private static readonly LocId ClimbStartMessage = "ladder-climb-start";
    private static readonly TimeSpan MinDelay = TimeSpan.FromSeconds(0.1);

    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly PullingSystem Pulling = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LadderComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<LadderComponent, StepTriggeredOnEvent>(OnStepTriggeredOn);
    }

    private void OnStepTriggerAttempt(Entity<LadderComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnStepTriggeredOn(Entity<LadderComponent> ent, ref StepTriggeredOnEvent args)
    {
        TryStartClimb(ent.AsNullable(), args.Tripper);
    }

    /// <summary>
    /// Пробует начать перенос существа по лестнице. Возвращает true, если ожидание началось.
    /// </summary>
    public bool TryStartClimb(Entity<LadderComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!CanClimb(ent, user))
            return false;

        return StartClimb((ent.Owner, ent.Comp), user);
    }

    /// <summary>
    /// Может ли существо перебраться по лестнице: лестница работает и куда-то ведёт, существо может двигаться,
    /// его никто не тащит и оно не прибыло на эту лестницу только что.
    /// </summary>
    public bool CanClimb(Entity<LadderComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!ent.Comp.Enabled || string.IsNullOrEmpty(ent.Comp.LinkId))
            return false;

        if (!HasComp<DoAfterComponent>(user))
            return false;

        if (IsArrivalIgnored(ent.Owner, user))
            return false;

        if (Pulling.IsPulled(user))
            return false;

        return _actionBlocker.CanMove(user);
    }

    /// <summary>
    /// Меняет ключ связи лестницы.
    /// </summary>
    public void SetLinkId(Entity<LadderComponent?> ent, string? linkId)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.LinkId == linkId)
            return;

        ent.Comp.LinkId = linkId;
        Dirty(ent.Owner, ent.Comp);
    }

    /// <summary>
    /// Включает или выключает лестницу.
    /// </summary>
    public void SetEnabled(Entity<LadderComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent.Owner, ent.Comp);
    }

    /// <summary>
    /// Помечает существо как только что прибывшее на лестницу, чтобы она не отправила его обратно.
    /// </summary>
    protected void MarkArrival(Entity<LadderComponent> ladder, EntityUid user)
    {
        var arrival = EnsureComp<LadderArrivalComponent>(user);
        arrival.Ladder = ladder.Owner;
        arrival.IgnoreUntil = Timing.CurTime + ladder.Comp.ArrivalGrace;
        Dirty(user, arrival);
    }

    /// <summary>
    /// Снимает метку прибытия, если она относится к этой лестнице.
    /// </summary>
    protected void ClearArrival(EntityUid ladder, EntityUid user)
    {
        if (TryComp<LadderArrivalComponent>(user, out var arrival) && arrival.Ladder == ladder)
            RemComp(user, arrival);
    }

    private bool IsArrivalIgnored(EntityUid ladder, EntityUid user)
    {
        return TryComp<LadderArrivalComponent>(user, out var arrival)
            && arrival.Ladder == ladder
            && Timing.CurTime < arrival.IgnoreUntil;
    }

    private bool StartClimb(Entity<LadderComponent> ent, EntityUid user)
    {
        var instant = ent.Comp.Delay <= MinDelay;
        var delay = instant ? MinDelay : ent.Comp.Delay;

        var args = new DoAfterArgs(EntityManager, user, delay, new LadderClimbDoAfterEvent(), ent.Owner, target: ent.Owner)
        {
            Hidden = instant,
            BreakOnMove = false,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = false,
            DistanceThreshold = ent.Comp.Range,
            BlockDuplicate = true,
            CancelDuplicate = false,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        if (!instant)
            Popup.PopupClient(Loc.GetString(ClimbStartMessage), ent.Owner, user);
        return true;
    }
}
