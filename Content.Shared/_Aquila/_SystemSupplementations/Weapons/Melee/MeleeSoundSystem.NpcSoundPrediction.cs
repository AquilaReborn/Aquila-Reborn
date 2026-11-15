using Content.Shared.NPC;

namespace Content.Shared.Weapons.Melee;

public sealed partial class MeleeSoundSystem
{
    private EntityUid? GetPredictingUser(EntityUid? user)
    {
        return user is { } uid && HasComp<ActiveNPCComponent>(uid) ? null : user;
    }
}
