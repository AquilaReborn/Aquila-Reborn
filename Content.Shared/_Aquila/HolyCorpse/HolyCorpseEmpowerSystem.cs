using Content.Goobstation.Common.Damage.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.Aquila.HolyCorpse;

public sealed class HolyCorpseEmpowerSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyCorpseEmpoweredComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<HolyCorpseEmpoweredComponent, GetMeleeAttackRateEvent>(OnGetMeleeAttackRate);
        SubscribeLocalEvent<HolyCorpseEmpoweredComponent, ModifyOutgoingStaminaDamageEvent>(OnModifyOutgoingStaminaDamage);
        SubscribeLocalEvent<HolyCorpseEmpoweredComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    /// <summary>
    /// Задаёт множитель усиления стража и пересчитывает модификаторы его оружия.
    /// </summary>
    public void SetMultiplier(EntityUid uid, float multiplier)
    {
        var comp = EnsureComp<HolyCorpseEmpoweredComponent>(uid);
        comp.Multiplier = multiplier;
        Dirty(uid, comp);

        if (TryComp<GunComponent>(uid, out var gun))
            _gun.RefreshModifiers((uid, gun));
    }

    private void OnGetMeleeDamage(Entity<HolyCorpseEmpoweredComponent> ent, ref GetMeleeDamageEvent args)
    {
        args.Damage *= ent.Comp.Multiplier;
    }

    private void OnGetMeleeAttackRate(Entity<HolyCorpseEmpoweredComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        args.Multipliers *= ent.Comp.Multiplier;
    }

    private void OnModifyOutgoingStaminaDamage(Entity<HolyCorpseEmpoweredComponent> ent, ref ModifyOutgoingStaminaDamageEvent args)
    {
        args.Value *= ent.Comp.Multiplier;
    }

    private void OnGunRefreshModifiers(Entity<HolyCorpseEmpoweredComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.FireRate *= ent.Comp.Multiplier;
    }
}
