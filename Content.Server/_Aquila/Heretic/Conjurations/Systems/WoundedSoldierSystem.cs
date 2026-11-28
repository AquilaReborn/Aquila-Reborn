using Content.Goobstation.Maths.FixedPoint;
using Content.Server._Aquila.Heretic.Conjurations.Components;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Heretic.Conjurations.Systems;

/// <summary>
/// Лечит носителя раненого солдата пропорционально тяжести механических и термических повреждений.
/// </summary>
public sealed class WoundedSoldierSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WoundedSoldierComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var soldier, out var damageable))
        {
            if (now < soldier.NextHeal)
                continue;

            soldier.NextHeal = now + soldier.HealInterval;

            HealGroup((uid, damageable), soldier, BruteGroup);
            HealGroup((uid, damageable), soldier, BurnGroup);
        }
    }

    private void HealGroup(Entity<DamageableComponent> ent,
        WoundedSoldierComponent soldier,
        ProtoId<DamageGroupPrototype> group)
    {
        var damage = _damageable.GetDamage(ent, group);
        var total = damage.GetTotal();
        if (total <= FixedPoint2.Zero)
            return;

        var healAmount = FixedPoint2.Clamp(total * soldier.HealFraction, soldier.MinHeal, soldier.MaxHeal);
        var scale = (healAmount / total).Float();

        var heal = new DamageSpecifier();
        foreach (var (type, value) in damage.DamageDict)
            heal.DamageDict[type] = -value * scale;

        _damageable.TryChangeDamage(ent.AsNullable(),
            heal,
            ignoreResistances: true,
            interruptsDoAfters: false,
            targetPart: TargetBodyPart.All,
            splitDamage: SplitDamageBehavior.SplitEnsureAll);
    }
}
