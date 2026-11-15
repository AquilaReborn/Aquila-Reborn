using Content.Shared.NPC;

namespace Content.Server.Weapons.Melee;

public sealed partial class MeleeWeaponSystem
{
    private bool IsDrivenByNpc(EntityUid user)
    {
        return HasComp<ActiveNPCComponent>(user);
    }
}
