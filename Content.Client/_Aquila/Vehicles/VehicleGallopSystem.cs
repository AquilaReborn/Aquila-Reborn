using Content.Goobstation.Shared.Vehicles;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Shared.Aquila.Vehicles;
using System.Numerics;
using Content.Client.IoC;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Client.ResourceManagement;

namespace Content.Client.Aquila.Vehicles;

public sealed class VehicleGallopSystem : SharedVehicleGallopSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VehicleGallopComponent, VehicleGallopSuccessEvent>(OnGallopSuccess);
        _overlay.AddOverlay(new VehicleGallopOverlay(EntityManager, _player, _resourceCache));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<VehicleGallopOverlay>();
    }

    private void OnGallopSuccess(EntityUid uid, VehicleGallopComponent component, VehicleGallopSuccessEvent args)
    {
        if (TerminatingOrDeleted(uid)
            || !_timing.IsFirstTimePredicted
            || component.SuccessCloudEffect == null)
            return;

        var coords = Transform(uid).Coordinates.Offset(component.CloudOffset);
        Spawn(component.SuccessCloudEffect, coords);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VehicleGallopComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Streak < 1
                || component.Rider == null
                || component.StepCloudEffect == null
                || !_timing.IsFirstTimePredicted
                || _timing.CurTime - component.LastStep < component.TimeBetweenSteps)
                continue;

            if (!TryComp<PhysicsComponent>(uid, out var physics)
                || physics.LinearVelocity.LengthSquared() < 0.01f)
                continue;

            var coords = Transform(uid).Coordinates.Offset(component.CloudOffset);
            Spawn(component.StepCloudEffect, coords);
            component.LastStep = _timing.CurTime;
        }
    }
}
