using Content.Server.Guardian;
using Content.Shared.Aquila.Spin;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Stunnable;
using Content.Server.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.StatusEffect;

namespace Content.Server.Aquila.Spin;

public sealed class SpinUserSystem : SharedSpinSystem
{
    [Dependency] private readonly GuardianSystem _guardian = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpinComponent, MeleeHitEvent>(OnMeleeHitServer);
        SubscribeLocalEvent<SpinUserComponent, SpinStunHitEvent>(OnStunHit);
        SubscribeLocalEvent<SpinUserComponent, SpinMarkHitEvent>(OnMarkHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpinMarkComponent>();
        while (query.MoveNext(out var uid, out var mark))
        {
            if (Timing.CurTime >= mark.EndTime)
            {
                QueueDel(uid);
                continue;
            }

            if (Timing.CurTime < mark.NextTickTime)
                continue;

            mark.NextTickTime = Timing.CurTime + mark.TickInterval;

            var coords = TransformSystem.GetMapCoordinates(uid);
            Spawn(mark.TickEffectProto, coords);

            var targets = _lookup.GetEntitiesInRange(coords, mark.AoeRadius);

            foreach (var target in targets)
            {
                if (HasComp<SpinUserComponent>(target))
                    continue;

                if (!TryComp(target, out StaminaComponent? stamina))
                    continue;

                _stamina.TakeStaminaDamage(target, mark.StaminaDamagePerTick, stamina);
            }
        }
    }

    private void OnMeleeHitServer(EntityUid uid, SpinComponent component, MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (!TryComp(target, out GuardianComponent? guardianComp))
                continue;

            if (guardianComp.Host == null)
                continue;

            _guardian.TryForceRetractGuardian(guardianComp.Host.Value);
        }
    }

    private void OnStunHit(EntityUid uid, SpinUserComponent comp, SpinStunHitEvent args)
    {
        if (!TryComp(args.Target, out StaminaComponent? _))
            return;

        var duration = comp.StunBlowStunTime;
        if (duration > TimeSpan.FromSeconds(5))
            duration = TimeSpan.FromSeconds(5);

        _stun.TryUpdateParalyzeDuration(args.Target, duration);
    }


    private void OnMarkHit(EntityUid uid, SpinUserComponent comp, SpinMarkHitEvent args)
    {
        var maxEnd = Timing.CurTime + TimeSpan.FromSeconds(8);

        EntityUid? existingMark = null;
        var enumerator = Transform(args.Target).ChildEnumerator;

        while (enumerator.MoveNext(out var child))
        {
            if (TryComp(child, out SpinMarkComponent? existingComp) &&
                existingComp.OwnerEntity == args.Performer)
            {
                existingMark = child;
                var newEnd = existingComp.EndTime + comp.MarkStackBonus;
                existingComp.EndTime = newEnd > maxEnd ? maxEnd : newEnd;
                Dirty(child, existingComp);
                break;
            }
        }

        if (existingMark != null)
            return;

        var targetXform = Transform(args.Target);
        var markEntity = Spawn(comp.MarkProto, targetXform.Coordinates);
        var markComp = EnsureComp<SpinMarkComponent>(markEntity);
        markComp.OwnerEntity = args.Performer;

        var initialEnd = Timing.CurTime + comp.MarkBaseDuration;
        markComp.EndTime = initialEnd > maxEnd ? maxEnd : initialEnd;
        markComp.NextTickTime = Timing.CurTime + markComp.TickInterval;
        Dirty(markEntity, markComp);

        TransformSystem.SetParent(markEntity, Transform(markEntity), args.Target, targetXform);
    }
}
