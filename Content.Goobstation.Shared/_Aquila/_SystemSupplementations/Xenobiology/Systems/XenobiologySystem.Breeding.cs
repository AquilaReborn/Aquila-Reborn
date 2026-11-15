using Content.Goobstation.Shared.Xenobiology.Components;

namespace Content.Goobstation.Shared.Xenobiology.Systems;

public partial class XenobiologySystem
{
    private void ApplyBreedName(EntityUid uid, BreedPrototype breed)
    {
        var breedName = Loc.GetString(breed.BreedName);
        if (TryComp<MobGrowthComponent>(uid, out var growth))
            growth.BaseEntityName = breedName;

        _meta.SetEntityName(uid, breedName);
    }
}
