using Content.Goobstation.Shared.Vehicles;
using Content.Server.Guardian;

namespace Content.Goobstation.Server.Aquila.Guardian;

public sealed partial class GuardianVehicleFollowSystem : EntitySystem
{
    [Dependency] private GuardianSystem _guardian = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnVehicleMove);
    }

    private void OnVehicleMove(EntityUid uid, VehicleComponent component, ref MoveEvent args)
    {
        if (component.Driver is not { } driver)
            return;

        if (!TryComp<GuardianHostComponent>(driver, out var hostComponent)
            || hostComponent.HostedGuardian is not { } guardian)
            return;

        if (!TryComp<GuardianComponent>(guardian, out var guardianComponent) || !guardianComponent.GuardianLoose)
            return;

        _guardian.CheckGuardianMove(driver, guardian, hostComponent, guardianComponent);
    }
}
