using Content.Shared._Aquila.PreCritical;
using Content.Shared.Mobs;

namespace Content.Server.Mobs;

public sealed partial class DeathgaspSystem
{
    private bool WasCriticalAquila(EntityUid uid, MobState state)
    {
        return state == MobState.Critical || state == MobState.Alive && HasComp<PreCriticalActiveComponent>(uid);
    }
}
