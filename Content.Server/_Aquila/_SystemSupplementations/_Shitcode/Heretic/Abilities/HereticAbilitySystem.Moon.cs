using Content.Goobstation.Shared.Illusion;
using Content.Shared._Aquila.Heretic.Moon.Events;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared.Heretic;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [Dependency] private readonly MoonSanitySystem _moonSanity = default!;

    protected override void SubscribeMoon()
    {
        base.SubscribeMoon();

        SubscribeLocalEvent<EventHereticMindGate>(OnMindGate);
        SubscribeLocalEvent<EventHereticLunarParade>(OnLunarParade);
        SubscribeLocalEvent<EventHereticRingleadersRise>(OnRingleadersRise);
    }

    private void OnMindGate(EventHereticMindGate args)
    {
        var target = args.Target;

        if (!HasComp<MobStateComponent>(target))
            return;

        if (!TryUseAbility(args))
            return;

        _moonSanity.ApplyBrainDamage(args.Performer, args.SelfDamage);
        _moonSanity.ApplyBrainDamage(target, args.TargetDamage);

        _statusNew.TryAddStatusEffectDuration(target, args.StatusEffect, args.EffectDuration);

        var targetCoordinates = Transform(target).Coordinates;
        SpawnAtPosition(args.Effect, targetCoordinates);
        SpawnAtPosition(args.BurstEffect, targetCoordinates);

        if (args.Sound is { } sound)
            _aud.PlayPvs(sound, target);
    }

    private void OnLunarParade(EventHereticLunarParade args)
    {
        if (!TryUseAbility(args))
            return;

        ShootProjectileSpell(args.Performer,
            args.Target,
            args.Projectile,
            args.ProjectileSpeed,
            null);
    }

    private void OnRingleadersRise(EventHereticRingleadersRise args)
    {
        if (!TryUseAbility(args))
            return;

        if (Heretic.TryGetHereticComponent(args.Performer, out var heretic, out var mind))
            SummonMoonIllusions((mind, heretic), args.Performer, args);
        else if (TryComp<HereticComponent>(args.Performer, out heretic))
            SummonMoonIllusions((args.Performer, heretic), args.Performer, args);
    }

    private void SummonMoonIllusions(Entity<HereticComponent?> ent, EntityUid user, EventHereticRingleadersRise args)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var targets = new List<EntityUid>();
        foreach (var target in GetNearbyPeople(user, args.Radius, "Moon"))
        {
            if (target.Comp.CurrentState != MobState.Dead)
                targets.Add(target.Owner);
        }

        var limit = ent.Comp.Ascended ? args.AscendedIllusionCount : args.IllusionCount;
        if (!CanSummonMoonIllusions(user, targets, limit))
            return;

        DoSummonMoonIllusions(ent.Owner, user, args, targets, limit);
    }

    private bool CanSummonMoonIllusions(EntityUid user, List<EntityUid> targets, int limit, bool quiet = false)
    {
        return !TerminatingOrDeleted(user) && targets.Count > 0 && limit > 0;
    }

    private void DoSummonMoonIllusions(EntityUid owner, EntityUid user, EventHereticRingleadersRise args,
        List<EntityUid> targets, int limit)
    {
        SpawnAtPosition(args.Effect, Transform(user).Coordinates);

        var ev = new SpawnIllusionEvent
        {
            Source = user,
            Count = limit,
            Lifetime = args.IllusionLifetime,
            HealthMultiplier = args.IllusionHealthMultiplier,
            Targets = targets,
            BudgetOwner = owner,
            ActiveLimit = limit,
            ExplosionType = args.IllusionExplosionType,
            ExplosionRadius = args.IllusionExplosionRadius,
        };

        RaiseLocalEvent(user, ref ev, true);
    }
}
