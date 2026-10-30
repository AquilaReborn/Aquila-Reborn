using Content.Server.Guardian;
using Content.Shared.Stunnable;

namespace Content.Server.Aquila.Guardian;

public sealed class GuardianAutoRetractSystem : EntitySystem
{
    [Dependency] private readonly GuardianSystem _guardian = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuardianHostComponent, StunnedEvent>(OnStunned);
    }

    private void OnStunned(Entity<GuardianHostComponent> ent, ref StunnedEvent args)
    {
        if (ent.Comp.HostedGuardian is not { } guardian)
            return;

        if (!TryComp<GuardianAutoRetractComponent>(guardian, out var autoRetract) || !autoRetract.RetractOnStun)
            return;

        _guardian.TryForceRetractGuardian(ent.AsNullable());
    }
}
