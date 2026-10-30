using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    /// <summary>
    /// Стреляет по координатам, не сбрасывая счётчик выстрелов, чтобы полуавтомат и очередь работали при зажатой кнопке.
    /// </summary>
    public bool AttemptShootHeld(EntityUid user, Entity<GunComponent> gun, EntityCoordinates toCoordinates, EntityUid? target = null)
    {
        gun.Comp.ShootCoordinates = toCoordinates;
        gun.Comp.Target = target;
        return AttemptShoot(user, gun);
    }

    /// <summary>
    /// Прекращает стрельбу из оружия, как при отпускании кнопки.
    /// </summary>
    public void StopShootingHeld(Entity<GunComponent> gun)
    {
        StopShooting(gun);
    }
}
