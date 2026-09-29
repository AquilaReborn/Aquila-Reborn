using Content.Goobstation.Shared.Vehicles;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Components;

using Robust.Shared.GameStates;

namespace Content.Shared.Aquila.Vehicles;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleSafetyComponent : Component
{
}

public sealed class VehicleSafetySystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleSafetyComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<VehicleSafetyComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnStrapAttempt(EntityUid uid, VehicleSafetyComponent component, ref StrapAttemptEvent args)
    {
        if (_mobState.IsIncapacitated(uid))
            args.Cancelled = true;
    }

    private void OnMobStateChanged(EntityUid uid, VehicleSafetyComponent component, MobStateChangedEvent args)
    {
        if (!TryComp<VehicleComponent>(uid, out var vehicleComp) || vehicleComp.Driver == null)
            return;

        if (args.NewMobState == MobState.Alive)
            return;

        _buckle.TryUnbuckle(vehicleComp.Driver.Value, vehicleComp.Driver.Value);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VehicleStaminaFallComponent, VehicleComponent, StaminaComponent>();
        while (query.MoveNext(out var uid, out var fallComp, out var vehicleComp, out var stamina))
        {
            var isCritical = stamina.StaminaDamage >= stamina.CritThreshold;

            if (isCritical && !fallComp.WasCritical && vehicleComp.Driver != null)
            {
                var rider = vehicleComp.Driver.Value;
                _buckle.TryUnbuckle(rider, rider);
                _stun.TryKnockdown(rider, TimeSpan.FromSeconds(fallComp.StunDuration), true);
            }

            fallComp.WasCritical = isCritical;
        }
    }
}
