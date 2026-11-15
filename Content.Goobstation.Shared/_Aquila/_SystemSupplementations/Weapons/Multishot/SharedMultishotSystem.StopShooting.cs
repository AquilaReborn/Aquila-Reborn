using Content.Shared.Weapons.Ranged.Events;

namespace Content.Goobstation.Shared.Weapons.Multishot;

public sealed partial class SharedMultishotSystem
{
    private void OnRequestStopShoot(RequestStopShootEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        foreach (var (gunEnt, gunComp, _) in GetMultishotGuns(user))
        {
            _gunSystem.StopShootingHeld((gunEnt, gunComp));
        }
    }
}
