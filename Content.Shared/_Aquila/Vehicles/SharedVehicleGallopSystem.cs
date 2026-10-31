using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage.Systems;

namespace Content.Shared.Aquila.Vehicles;

public abstract partial class SharedVehicleGallopSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleGallopComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleGallopComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<VehicleGallopComponent, GallopActionEvent>(OnGallopAction);
        SubscribeLocalEvent<VehicleGallopComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<VehicleGallopComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnStrapped(Entity<VehicleGallopComponent> ent, ref StrappedEvent args)
    {
        EnsureComp<MovementSpeedModifierComponent>(ent);

        var comp = ent.Comp;
        comp.Rider = args.Buckle.Owner;
        comp.Streak = 0;
        comp.CurrentMultiplier = 1f;
        comp.NextDecayTime = TimeSpan.Zero;
        Dirty(ent);

        _actions.AddAction(args.Buckle.Owner, ref comp.GallopAction, comp.GallopActionProto, ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnUnstrapped(Entity<VehicleGallopComponent> ent, ref UnstrappedEvent args)
    {
        var comp = ent.Comp;

        if (comp.GallopAction != null)
            _actions.RemoveAction(args.Buckle.Owner, comp.GallopAction);

        comp.Rider = null;
        comp.Streak = 0;
        comp.CurrentMultiplier = 1f;
        comp.NextDecayTime = TimeSpan.Zero;
        Dirty(ent);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnGallopAction(Entity<VehicleGallopComponent> ent, ref GallopActionEvent args)
    {
        if (args.Handled)
            return;

        var comp = ent.Comp;

        if (comp.Rider != args.Performer)
            return;
        args.Handled = true;
        comp.Streak = Math.Min(comp.MaxStreak, comp.Streak + 1);
        comp.CurrentMultiplier = StreakToMultiplier(comp);
        comp.NextDecayTime = _timing.CurTime + comp.FirstDecayDelay;
        Dirty(ent);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        _stamina.TakeStaminaDamage(ent, comp.StaminaCost);

        if (comp.GallopSuccessSound != null)
            _audio.PlayPredicted(comp.GallopSuccessSound, ent, args.Performer);

        RaiseLocalEvent(ent.Owner, new VehicleGallopSuccessEvent(args.Performer));
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

    private void OnHandleState(Entity<VehicleGallopComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<VehicleGallopComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Streak <= 0 || now < comp.NextDecayTime)
                continue;

            comp.Streak--;
            comp.CurrentMultiplier = StreakToMultiplier(comp);
            comp.NextDecayTime = now + comp.NextDecayDelay;
            Dirty(uid, comp);

            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }
    }
}
