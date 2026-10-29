using Content.Shared._Goobstation.Wizard.Guardian;

namespace Content.Shared.Projectiles;

public abstract partial class SharedProjectileSystem
{
    private bool IsShotAtGuardianHost(ProjectileComponent component, EntityUid target)
    {
        return component.Shooter is { } shooter
               && TryComp<GuardianSharedComponent>(shooter, out var guardianShared)
               && guardianShared.Host == target;
    }
}
