using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;

namespace Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    private bool TryRestoreHoldingWoundable(Entity<WoundComponent> wound)
    {
        if (wound.Comp.HoldingWoundable != EntityUid.Invalid)
            return true;

        if (!_container.TryGetContainingContainer((wound.Owner, null, null), out var container)
            || container.ID != WoundContainerId
            || !HasComp<WoundableComponent>(container.Owner))
            return false;

        wound.Comp.HoldingWoundable = container.Owner;
        Dirty(wound);
        return true;
    }
}
