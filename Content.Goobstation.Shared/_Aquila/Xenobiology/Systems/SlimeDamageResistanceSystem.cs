using Content.Goobstation.Shared._Aquila.Xenobiology.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.Systems;

public sealed partial class SlimeDamageResistanceSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeDamageResistanceComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<SlimeDamageResistanceComponent> ent, ref DamageModifyEvent args)
    {
        if (_proto.Resolve(ent.Comp.ModifierSet, out var modifierSet))
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifierSet);
    }
}
