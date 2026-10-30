using Robust.Shared.Containers;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    private void CycleExisting(EntityUid existing, BaseContainer container)
    {
        if (TerminatingOrDeleted(existing))
            return;

        Containers.Remove(existing, container);
        EnsureShootable(existing);
    }
}
