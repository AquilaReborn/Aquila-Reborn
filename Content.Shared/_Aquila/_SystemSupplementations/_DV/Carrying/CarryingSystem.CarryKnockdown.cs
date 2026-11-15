using Content.Goobstation.Common.Traits;
using Content.Shared._Aquila.PreCritical;
using Content.Shared.Stunnable;

namespace Content.Shared._DV.Carrying;

public sealed partial class CarryingSystem
{
    private void InitializeCarryKnockdownAquila()
    {
        SubscribeLocalEvent<BeingCarriedComponent, KnockedDownEvent>(OnKnockedDownAquila);
    }

    private bool IsCrawlingAquila(EntityUid uid)
    {
        return HasComp<KnockedDownComponent>(uid) || HasComp<PreCriticalActiveComponent>(uid);
    }

    private void ApplyCarryKnockdownAquila(Entity<BeingCarriedComponent> ent)
    {
        if (IsCrawlingAquila(ent.Owner))
            return;

        ent.Comp.OwnsKnockdown = true;
        EnsureComp<KnockedDownComponent>(ent);
        Dirty(ent);
    }

    private void RemoveCarryKnockdownAquila(EntityUid uid)
    {
        var ownsKnockdown = TryComp<BeingCarriedComponent>(uid, out var carried) && carried.OwnsKnockdown;
        RemComp<BeingCarriedComponent>(uid);

        if (ownsKnockdown && !HasComp<LegsParalyzedComponent>(uid) &&
            TryComp<KnockedDownComponent>(uid, out var knockdown) && knockdown.NextUpdate == TimeSpan.Zero)
        {
            RemComp<KnockedDownComponent>(uid);
        }
    }

    private void OnKnockedDownAquila(Entity<BeingCarriedComponent> ent, ref KnockedDownEvent args)
    {
        if (!ent.Comp.OwnsKnockdown)
            return;

        ent.Comp.OwnsKnockdown = false;
        Dirty(ent);
    }
}
