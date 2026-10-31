using Content.Server.Guardian;
using Content.Shared.Aquila.Spin;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Aquila.Spin;

public sealed class SpinSystem : SharedSpinSystem
{
    [Dependency] private readonly GuardianSystem _guardian = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpinComponent, MeleeHitEvent>(OnMeleeHitServer);
    }

    private void OnMeleeHitServer(EntityUid uid, SpinComponent component, MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (!TryComp(target, out GuardianComponent? guardianComp))
                continue;

            if (guardianComp.Host == null)
                continue;

            _guardian.TryForceRetractGuardian(guardianComp.Host.Value);
        }
    }
}
