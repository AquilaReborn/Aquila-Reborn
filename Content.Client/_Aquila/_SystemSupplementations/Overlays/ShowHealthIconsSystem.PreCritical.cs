using Content.Shared._Aquila.PreCritical;
using Content.Shared.Mobs;

namespace Content.Client.Overlays;

public sealed partial class ShowHealthIconsSystem
{
    private MobState GetHealthIconStateAquila(EntityUid uid, MobState state)
    {
        return state == MobState.Alive && HasComp<PreCriticalActiveComponent>(uid) ? MobState.Critical : state;
    }
}
