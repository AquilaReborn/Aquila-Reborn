using Content.Goobstation.Shared.Vehicles;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;

namespace Content.Goobstation.Shared.Aquila.Vehicles;

public sealed class VehicleProjectileImmunitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(EntityUid uid, VehicleComponent component, ref PreventCollideEvent args)
    {
        if (component.Driver == null)
            return;

        var other = args.OtherEntity;

        if (TryComp<ProjectileComponent>(other, out var projectile)
            && projectile.Shooter == component.Driver)
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<ThrownItemComponent>(other, out var thrown)
            && thrown.Thrower == component.Driver)
        {
            args.Cancelled = true;
        }
    }
}
