using Content.Shared.Abilities;
using Robust.Shared.Spawners;

namespace Content.Goobstation.Shared.Sandevistan;

public sealed partial class SandevistanSystem
{
    private void SetupAfterimageFade(EntityUid afterimage, SandevistanAfterimageComponent afterimageComp, SandevistanUserComponent userComp)
    {
        afterimageComp.SpawnTime = _timing.CurTime;
        afterimageComp.Lifetime = userComp.AfterimageLifetime;
        EnsureComp<TimedDespawnComponent>(afterimage).Lifetime = userComp.AfterimageLifetime;
    }

    private void AddTimeVision(Entity<SandevistanUserComponent> ent)
    {
        if (ent.Comp.TimeVision)
            EnsureComp<DogVisionComponent>(ent);
    }

    private void RemoveTimeVision(EntityUid uid, SandevistanUserComponent comp)
    {
        if (comp.TimeVision)
            RemCompDeferred<DogVisionComponent>(uid);
    }
}
