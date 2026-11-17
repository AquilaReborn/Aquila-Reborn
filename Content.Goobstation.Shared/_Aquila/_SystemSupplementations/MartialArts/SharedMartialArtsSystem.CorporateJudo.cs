using Content.Goobstation.Shared.MartialArts.Components;
using Content.Shared.Weapons.Melee;

namespace Content.Goobstation.Shared.MartialArts;

public abstract partial class SharedMartialArtsSystem
{
    /// <summary>
    /// Выдаёт пользователю корпоративное дзюдо.
    /// </summary>
    /// <param name="user">Сущность, которой выдаётся дзюдо.</param>
    /// <returns><see langword="true"/>, если дзюдо выдано.</returns>
    public bool TryGrantCorporateJudo(EntityUid user)
    {
        if (!HasComp<MeleeWeaponComponent>(user))
            return false;

        return TryGrantMartialArt(user, new GrantCorporateJudoComponent());
    }
}
