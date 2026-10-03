using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Aquila.Vehicles;

public abstract partial class SharedVehicleGallopSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly EntProtoId GallopActionId = "ActionGallop";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleGallopComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleGallopComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<VehicleGallopComponent, GallopActionEvent>(OnGallopAction);
        SubscribeLocalEvent<VehicleGallopComponent, GallopDoAfterEvent>(OnDoAfterFinished);
        SubscribeLocalEvent<VehicleGallopComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void CancelActiveDoAfter(Entity<VehicleGallopComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.ActiveDoAfter == null)
            return;

        if (_doAfter.IsRunning(comp.ActiveDoAfter))
            _doAfter.Cancel(comp.ActiveDoAfter);

        comp.ActiveDoAfter = null;
        Dirty(ent);
    }

    private void OnStrapped(Entity<VehicleGallopComponent> ent, ref StrappedEvent args)
    {
        EnsureComp<MovementSpeedModifierComponent>(ent);

        CancelActiveDoAfter(ent);

        var comp = ent.Comp;
        comp.Rider = args.Buckle.Owner;
        comp.Streak = 0;
        comp.CurrentMultiplier = 1f;
        comp.State = GallopState.Idle;
        Dirty(ent);

        _actions.AddAction(args.Buckle.Owner, ref comp.GallopAction, GallopActionId, ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnUnstrapped(Entity<VehicleGallopComponent> ent, ref UnstrappedEvent args)
    {
        var comp = ent.Comp;

        if (comp.GallopAction != null)
            _actions.RemoveAction(args.Buckle.Owner, comp.GallopAction);

        CancelActiveDoAfter(ent);

        comp.Rider = null;
        comp.Streak = 0;
        comp.CurrentMultiplier = 1f;
        comp.State = GallopState.Idle;
        Dirty(ent);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private static float GetCurrentInterval(VehicleGallopComponent comp)
    {
        var value = comp.Interval - comp.IntervalStepPerStreak * comp.Streak;
        return Math.Max(comp.MinInterval, value);
    }

    private static float GetCurrentWindowLength(VehicleGallopComponent comp)
    {
        var value = comp.WindowLength - comp.WindowLengthStepPerStreak * comp.Streak;
        return Math.Max(comp.MinWindowLength, value);
    }

    private void StartWaitingPhase(Entity<VehicleGallopComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.Rider is not { } rider)
        {
            comp.State = GallopState.Idle;
            Dirty(ent);
            return;
        }

        var interval = GetCurrentInterval(comp);

        var doAfterArgs = new DoAfterArgs(EntityManager, rider, interval, new GallopDoAfterEvent(), ent, ent)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            NeedHand = false,
            Hidden = true,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId))
        {
            comp.State = GallopState.Waiting;
            comp.ActiveDoAfter = doAfterId;
        }
        else
        {
            comp.State = GallopState.Idle;
        }

        Dirty(ent);
    }

    private void OnDoAfterFinished(Entity<VehicleGallopComponent> ent, ref GallopDoAfterEvent args)
    {
        var comp = ent.Comp;
        comp.ActiveDoAfter = null;

        if (args.Cancelled)
        {
            Dirty(ent);
            return;
        }

        comp.State = GallopState.WindowOpen;
        comp.WindowEnd = _timing.CurTime + TimeSpan.FromSeconds(GetCurrentWindowLength(comp));
        Dirty(ent);
    }

    private void OnGallopAction(Entity<VehicleGallopComponent> ent, ref GallopActionEvent args)
    {
        if (args.Handled)
            return;

        var comp = ent.Comp;

        if (comp.Rider != args.Performer)
            return;

        switch (comp.State)
        {
            case GallopState.Idle:
                StartWaitingPhase(ent);
            break;

            case GallopState.Waiting:
                CancelActiveDoAfter(ent);
                ApplySlowdown(ent);
            break;

            case GallopState.WindowOpen:
                comp.Streak = Math.Min(comp.MaxStreak, comp.Streak + 1);
                comp.CurrentMultiplier = StreakToMultiplier(comp);
                Dirty(ent);
                _movementSpeed.RefreshMovementSpeedModifiers(ent);

                if (comp.GallopSuccessSound != null)
                    _audio.PlayPredicted(comp.GallopSuccessSound, ent, args.Performer);

                if (comp.Rider != null)
                    RaiseLocalEvent(ent.Owner, new VehicleGallopSuccessEvent(comp.Rider.Value));

                StartWaitingPhase(ent);
            break;

            case GallopState.Slowed:
                // попа
            break;
        }

        args.Handled = true;
    }

    private void ApplySlowdown(Entity<VehicleGallopComponent> ent)
    {
        var comp = ent.Comp;
        comp.State = GallopState.Slowed;
        comp.Streak = 0;
        comp.CurrentMultiplier = comp.SlowdownMultiplier;
        comp.SlowEnd = _timing.CurTime + TimeSpan.FromSeconds(comp.SlowdownDuration);
        Dirty(ent);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        if (comp.Rider != null)
            _popup.PopupPredicted("Слишком рано!", $"{Name(comp.Rider.Value)} чуть не свалился с лошади!", ent, comp.Rider.Value, PopupType.MediumCaution); // локализация
    }

    private static float StreakToMultiplier(VehicleGallopComponent comp)
    {
        if (comp.MaxStreak <= 0)
            return 1f;

        var step = (comp.MaxMultiplier - 1f) / comp.MaxStreak;
        return 1f + step * comp.Streak;
    }

    private void OnRefreshSpeed(Entity<VehicleGallopComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.CurrentMultiplier, ent.Comp.CurrentMultiplier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<VehicleGallopComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            switch (comp.State)
            {
                case GallopState.WindowOpen when now > comp.WindowEnd:
                    comp.Streak = 0;
                    comp.CurrentMultiplier = 1f;
                    comp.State = GallopState.Idle;
                    Dirty(uid, comp);
                    _movementSpeed.RefreshMovementSpeedModifiers(uid);
                break;

                case GallopState.Slowed when now > comp.SlowEnd:
                    comp.CurrentMultiplier = 1f;
                    comp.State = GallopState.Idle;
                    Dirty(uid, comp);
                    _movementSpeed.RefreshMovementSpeedModifiers(uid);
                break;
            }
        }
    }
}
