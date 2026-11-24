using Content.Goobstation.Shared.Enchanting.Components;

namespace Content.Goobstation.Shared.Enchanting.Systems;

public sealed partial class EnchanterSystem
{
    private bool IsEnchantingTableTarget(EntityUid target)
    {
        return HasComp<EnchantingTableComponent>(target);
    }
}
