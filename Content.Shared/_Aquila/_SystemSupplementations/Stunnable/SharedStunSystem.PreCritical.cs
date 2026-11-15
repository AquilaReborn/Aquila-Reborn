using Content.Shared._Aquila.PreCritical;

namespace Content.Shared.Stunnable;

public abstract partial class SharedStunSystem
{
    private bool IsPreCriticalAquila(EntityUid uid)
    {
        return HasComp<PreCriticalActiveComponent>(uid);
    }

    private float KnockdownMovementModifierAquila(EntityUid uid, float modifier)
    {
        return IsPreCriticalAquila(uid) ? 1f : modifier;
    }
}
