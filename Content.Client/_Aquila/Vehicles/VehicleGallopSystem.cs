using Content.Goobstation.Shared.Vehicles;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Shared.Aquila.Vehicles;
using System.Numerics;

namespace Content.Client.Aquila.Vehicles;

public sealed class VehicleGallopSystem : SharedVehicleGallopSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly Vector2 CloudOffset = new(0f, -0.55f);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleGallopComponent, VehicleGallopSuccessEvent>(OnGallopSuccess);
    }

    private void OnGallopSuccess(EntityUid uid, VehicleGallopComponent component, VehicleGallopSuccessEvent args)
    {
        if (TerminatingOrDeleted(uid)
            || !_timing.IsFirstTimePredicted
            || component.SuccessCloudEffect == null)
            return;

        var coords = Transform(uid).Coordinates.Offset(CloudOffset);
        Spawn(component.SuccessCloudEffect, coords);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VehicleGallopComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Streak < 1
                || component.State is not (GallopState.Waiting or GallopState.WindowOpen)
                || component.StepCloudEffect == null
                || !_timing.IsFirstTimePredicted
                || _timing.CurTime - component.LastStep < component.TimeBetweenSteps)
                continue;

            if (!TryComp<PhysicsComponent>(uid, out var physics)
                || physics.LinearVelocity.LengthSquared() < 0.01f)
                continue;

            var coords = Transform(uid).Coordinates.Offset(CloudOffset);
            Spawn(component.StepCloudEffect, coords);
            component.LastStep = _timing.CurTime;
        }
    }
}
