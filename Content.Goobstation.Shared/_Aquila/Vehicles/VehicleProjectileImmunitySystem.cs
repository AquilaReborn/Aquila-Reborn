using Content.Goobstation.Shared.Vehicles;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;
using Content.Shared._Goobstation.Wizard.Guardian;

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
        var driver = component.Driver.Value;

        if (TryComp<ProjectileComponent>(other, out var projectile)
            && (projectile.Shooter == driver || IsGuardianOfHost(projectile.Shooter, driver)))
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<ThrownItemComponent>(other, out var thrown)
            && thrown.Thrower == driver)
        {
            args.Cancelled = true;
        }
    }

    private bool IsGuardianOfHost(EntityUid? shooter, EntityUid host)
    {
        return shooter != null
            && TryComp<GuardianSharedComponent>(shooter.Value, out var guardianShared)
            && guardianShared.Host == host;
    }
}
