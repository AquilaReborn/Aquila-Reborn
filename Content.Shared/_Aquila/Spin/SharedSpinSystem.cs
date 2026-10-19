using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Projectiles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Throwing;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Shared.Aquila.Spin;

public abstract class SharedSpinSystem : EntitySystem
{
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpinUserComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SpinUserComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpinSummonEvent>(OnSpinSummon);
        SubscribeLocalEvent<SpinComponent, ThrowDoHitEvent>(OnSpinThrowHit);
        SubscribeLocalEvent<SpinEmpowerEvent>(OnSpinEmpower);
        SubscribeLocalEvent<SpinEmpowerCastingComponent, SpinEmpowerDoAfterEvent>(OnSpinEmpowerDoAfter);
    }

    private static readonly TimeSpan EmpowerBaseDelay = TimeSpan.FromSeconds(1.5);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<SpinEmpoweredComponent>();
        while (query.MoveNext(out var uid, out var empowered))
        {
            if (Timing.CurTime<empowered.EndTime)
                continue;

            RevertEmpower(uid, empowered);
        }
    }

    private void OnStartup(Entity<SpinUserComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Action != null)
            return;

        Actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionProto);
        Dirty(ent, ent.Comp);
    }

    private void OnShutdown(Entity<SpinUserComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Action != null)
            Actions.RemoveAction(ent.Owner, ent.Comp.Action);
    }

    private void OnSpinSummon(SpinSummonEvent ev)
    {
        if (ev.Handled)
            return;

        if (!TryComp(ev.Performer, out SpinUserComponent? spinUser))
            return;

        Hands.TryGetActiveItem(ev.Performer, out var held);

        if (held != null && held == spinUser.BoundEntity)
            return;

        if (spinUser.BoundEntity == null || !Exists(spinUser.BoundEntity) ||
            !TryComp(spinUser.BoundEntity.Value, out TransformComponent? xform))
        {
            if (ItemValid(held) && HasComp<SpinComponent>(held))
                MarkItem(held.Value);

            return;
        }

        if (ItemValid(held))
        {
            if (HasComp<SpinComponent>(held))
                MarkItem(held.Value);

            return;
        }

        ev.Handled = true;

        if (_net.IsClient)
            return;

        var item = spinUser.BoundEntity.Value;

        if (TryGetOuterNonMobContainer(item, xform!, out var container))
            item = container.Owner;

        Audio.PlayEntity(spinUser.SummonSound, Filter.Pvs(item).Merge(Filter.Pvs(ev.Performer)), item, true);

        if (TryComp(item, out EmbeddableProjectileComponent? embeddable) && embeddable.EmbeddedIntoUid != null)
            _projectile.EmbedDetach(item, embeddable);

        TransformSystem.SetMapCoordinates(item, TransformSystem.GetMapCoordinates(ev.Performer));
        TransformSystem.AttachToGridOrMap(item);

        Hands.TryForcePickupAnyHand(ev.Performer, item);

        return;

        void MarkItem(EntityUid obj)
        {
            spinUser.BoundEntity = obj;
            Dirty(ev.Performer, spinUser);
        }

        bool ItemValid([NotNullWhen(true)] EntityUid? obj)
        {
            return HasComp<ItemComponent>(obj) && !HasComp<VirtualItemComponent>(obj);
        }
    }

    private void OnSpinThrowHit(EntityUid uid, SpinComponent component, ThrowDoHitEvent args)
    {
        if (_net.IsClient)
            return;

        if (!HasComp<MobStateComponent>(args.Target))
           return;

        var thrower = args.Component.Thrower;
        if (thrower == null || !TryComp(thrower.Value, out SpinUserComponent? spinUser))
            return;

        if (spinUser.BoundEntity != uid)
            return;

        if (spinUser.Charges >= spinUser.MaxCharges)
            return;

        if (Timing.CurTime<spinUser.NextChargeTime)
            return;

        spinUser.Charges++;
        spinUser.NextChargeTime = Timing.CurTime + TimeSpan.FromSeconds(1);
        UpdateEmpowerAction(thrower.Value, spinUser);
        Dirty(thrower.Value, spinUser);

        if (spinUser.ChargeSound != null &&
            _playerManager.TryGetSessionByEntity(thrower.Value, out var chargeSession))
        {
            Audio.PlayGlobal(spinUser.ChargeSound, Filter.SinglePlayer(chargeSession), true);
        }

        var hitCoords = TransformSystem.GetMapCoordinates(uid);
        var hitAngle = TransformSystem.GetWorldRotation(uid);
        var hitEffect = Spawn(spinUser.HitEffectProto, hitCoords);
        TransformSystem.SetWorldRotation(hitEffect, hitAngle);
    }

    private void OnSpinEmpower(SpinEmpowerEvent ev)
    {
        if (ev.Handled)
            return;

        if (!TryComp(ev.Performer, out SpinUserComponent? spinUser))
            return;

        if (spinUser.Charges<spinUser.MaxCharges)
            return;

        if (HasComp<SpinEmpowerCastingComponent>(ev.Performer))
            return;

        ev.Handled = true;

        if (spinUser.BoundEntity == null || !Exists(spinUser.BoundEntity))
        {
            _popup.PopupClient(Loc.GetString("spin-empower-no-bound-item"), ev.Performer, ev.Performer);
            return;
        }

        var casting = EnsureComp<SpinEmpowerCastingComponent>(ev.Performer);
        StartEmpowerStep(ev.Performer, casting, 1);
    }

    private void StartEmpowerStep(EntityUid performer, SpinEmpowerCastingComponent casting, int step)
    {
        var newCasting = EnsureComp<SpinEmpowerCastingComponent>(performer);
        newCasting.Step = step;

        var delay = EmpowerBaseDelay * MathF.Pow(1.6f, step - 1);

        var args = new DoAfterArgs(EntityManager,
    performer,
    delay,
    new SpinEmpowerDoAfterEvent(step),
    performer)
            {
        BreakOnMove = false,
BreakOnDamage = true,
NeedHand = false,
        };

        if (!DoAfterSystem.TryStartDoAfter(args, out var id))
                    {
            RemCompDeferred<SpinEmpowerCastingComponent>(performer);
                        return;
                    }

        newCasting.DoAfterId = id.Value.Index;
    }

    private void OnSpinEmpowerDoAfter(Entity<SpinEmpowerCastingComponent> ent, ref SpinEmpowerDoAfterEvent args)
    {

        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled)
        {
            RemCompDeferred<SpinEmpowerCastingComponent>(ent);
            ApplyEmpowerCancelPenalty(ent, args.Step);
            return;
        }

        switch (args.Step)
        {
            case 1:
                _popup.PopupClient(Loc.GetString("spin-empower-step-1"), ent, ent, PopupType.Medium);
            break;

            case 2:
                _popup.PopupClient(Loc.GetString("spin-empower-step-2"), ent, ent, PopupType.Medium);
            break;
        }

        if (args.Step < 3)
        {
            RemCompDeferred<SpinEmpowerCastingComponent>(ent);
            StartEmpowerStep(ent, ent.Comp, args.Step + 1);
                    return;
        }

        RemCompDeferred<SpinEmpowerCastingComponent>(ent);
        _popup.PopupClient(Loc.GetString("spin-empower-step-3"), ent, ent, PopupType.Medium);
        FinishEmpower(ent);
    }

    private void ApplyEmpowerCancelPenalty(EntityUid performer, int step)
    {
        if (!TryComp(performer, out SpinUserComponent? spinUser))
            return;

        var penalty = step switch
        {
            1 => 3,
            2 => 5,
            _ => 7,
        };

        spinUser.Charges = Math.Max(0, spinUser.Charges - penalty);
        UpdateEmpowerAction(performer, spinUser);
        Dirty(performer, spinUser);
    }

    private void FinishEmpower(EntityUid performer)
    {
            if (!TryComp(performer, out SpinUserComponent? spinUser))
                    return;
    
            if (spinUser.BoundEntity == null || !Exists(spinUser.BoundEntity))
                    return;
    
    var item = spinUser.BoundEntity.Value;
    
    spinUser.Charges = 0;
    UpdateEmpowerAction(performer, spinUser);
    Dirty(performer, spinUser);
    
            if (_net.IsClient)
                    return;
    
    var empowered = EnsureComp<SpinEmpoweredComponent>(item);
    var stamina = EnsureComp<StaminaDamageOnCollideComponent>(item);
    var hit = EnsureComp<DamageOtherOnHitComponent>(item);
    
    empowered.OriginalStaminaDamage = stamina.Damage;
    empowered.OriginalHitDamage = hit.Damage;
    empowered.EndTime = Timing.CurTime + spinUser.EmpowerDuration;
    
    stamina.Damage = 200f;
    
    Dirty(item, empowered);

        var itemXform = Transform(item);
        var itemEffect = Spawn(spinUser.EmpowerItemEffectProto, itemXform.Coordinates);
        TransformSystem.SetParent(itemEffect, Transform(itemEffect), item, itemXform);
        empowered.Effect = itemEffect;

        var performerXform = Transform(performer);
        var userEffect = Spawn(spinUser.EmpowerUserEffectProto, performerXform.Coordinates);
        TransformSystem.SetParent(userEffect, Transform(userEffect), performer, performerXform);

    }

    private void RevertEmpower(EntityUid uid, SpinEmpoweredComponent empowered)
    {
        if (TryComp(uid, out StaminaDamageOnCollideComponent? stamina))
        {
            stamina.Damage = empowered.OriginalStaminaDamage;
        }
    

        if (empowered.Effect != null)
            QueueDel(empowered.Effect.Value);

        RemComp<SpinEmpoweredComponent>(uid);
    }

    private void UpdateEmpowerAction(EntityUid uid, SpinUserComponent comp)
    {
            if (comp.Charges >= comp.MaxCharges)
                {
                    if (comp.EmpowerAction == null)
            Actions.AddAction(uid, ref comp.EmpowerAction, comp.EmpowerActionProto);
                }
            else if (comp.EmpowerAction != null)
                {
        Actions.RemoveAction(uid, comp.EmpowerAction);
        comp.EmpowerAction = null;
                }
    }

    private bool TryGetOuterNonMobContainer(EntityUid uid,
        TransformComponent xform,
        [NotNullWhen(true)] out BaseContainer? container)
    {
        container = null;
        return false;
    }
}
