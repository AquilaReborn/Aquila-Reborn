using Content.Server.Aquila.LifeCreator;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Aquila.LifeCreator;

public sealed class ReflectMeleeDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var totalDamage = args.BaseDamage + args.BonusDamage;

        foreach (var hitEntity in args.HitEntities)
        {
            if (hitEntity == args.User)
                continue;

            if (!HasComp<ReflectMeleeDamageComponent>(hitEntity))
                continue;

            _damageable.TryChangeDamage(args.User, totalDamage, ignoreResistances: true, origin: hitEntity);
        }
    }
}
