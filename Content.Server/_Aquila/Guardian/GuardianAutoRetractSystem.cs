using Content.Shared.Stunnable;
using Content.Server.Guardian;

namespace Content.Server.Aquila.Guardian;

public sealed class GuardianAutoRetractSystem : EntitySystem
{
    [Dependency] private readonly GuardianSystem _guardian = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GuardianHostComponent, StunnedEvent>(OnHostStunned);
    }

    private void OnHostStunned(EntityUid uid, GuardianHostComponent hostComponent, ref StunnedEvent args)
    {
        if (hostComponent.HostedGuardian is not { } guardian)
            return;

        if (!TryComp<GuardianAutoRetractComponent>(guardian, out var autoRetract) || !autoRetract.RetractOnStun)
            return;

        _guardian.TryForceRetractGuardian(uid);
    }
}
