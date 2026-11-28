using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Traumas;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Обслуживает пассивную силу пути Луны: восстанавливает мозг, снимает эффекты сна,
/// не даёт получить травму мозга.
/// </summary>
public sealed class MoonPassiveSystem : SharedMoonPassiveSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MoonSanitySystem _sanity = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private static readonly EntProtoId DrowsinessStatus = "StatusEffectDrowsiness";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoonPassiveComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<WoundableComponent, BeforeTraumaInducedEvent>(OnBeforeTraumaInduced);
    }

    private void OnDamaged(Entity<MoonPassiveComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        ent.Comp.CombatEndTime = _timing.CurTime + ent.Comp.CombatTimeout;
    }

    private void OnBeforeTraumaInduced(Entity<WoundableComponent> ent, ref BeforeTraumaInducedEvent args)
    {
        if (args.TraumaType != TraumaSystem.OrganDamage || !HasComp<BrainComponent>(args.TraumaTarget))
            return;

        if (!TryComp<BodyPartComponent>(ent.Owner, out var part) || part.Body is not { } body)
            return;

        if (!HasComp<MoonPassiveComponent>(body))
            return;

        args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MoonPassiveComponent>();
        while (query.MoveNext(out var uid, out var passive))
        {
            if (now >= passive.NextSleepImmunity)
            {
                passive.NextSleepImmunity = now + passive.SleepImmunityInterval;
                _status.TryRemoveStatusEffect(uid, SleepingSystem.StatusEffectForcedSleeping);
                _status.TryRemoveStatusEffect(uid, DrowsinessStatus);
            }

            if (now < passive.NextHeal)
                continue;

            passive.NextHeal = now + passive.HealInterval;

            var heal = passive.HealAmount;
            if (now < passive.CombatEndTime)
                heal = FixedPoint2.New(heal.Float() * passive.CombatHealMultiplier);

            _sanity.HealBrainDamage(uid, heal);
            _sanity.RemoveBrainTraumas(uid);
        }
    }
}
