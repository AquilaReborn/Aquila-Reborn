using System.Linq;
using System.Numerics;
using Content.Shared.Aquila.Barriers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server.Aquila.Barriers;

public sealed class BarrierSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly PhysicsSystem _physicsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarrierSpawnActionEvent>(OnSpawnAction);
        SubscribeNetworkEvent<BarrierOrientationToggleEvent>(OnOrientationToggle);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toSpawn = new List<(EntProtoId Proto, MapCoordinates Coords)>();

        var enumerator = EntityQueryEnumerator<BarrierComponent>();
        while (enumerator.MoveNext(out var uid, out var component))
        {
            foreach (var segment in component.PendingSegments.ToList())
            {
                if (segment.Time > _timing.CurTime)
                    continue;

                component.PendingSegments.Remove(segment);

                var protoId = MetaData(uid).EntityPrototype?.ID;
                if (protoId == null)
                    continue;

                toSpawn.Add((protoId, segment.Coords));
            }
        }

        foreach (var (proto, coords) in toSpawn)
        {
            Spawn(proto, coords);
        }
    }

    private void OnSpawnAction(BarrierSpawnActionEvent args)
    {
        if (args.Handled) return;
        args.Handled = true;

        var isVertical = TryComp<BarrierOrientationComponent>(args.Action, out var orientation) && orientation.IsVertical;
        var direction = isVertical ? new Vector2(0, 1) : new Vector2(1, 0);

        var originCoords = _transformSystem.ToMapCoordinates(args.Target);

        var ent = Spawn(args.SpawnedEntityPrototype, originCoords);

        if (!TryComp<BarrierComponent>(ent, out var barrierComponent))
            return;

        for (var i = 1; i <= barrierComponent.BarrierLength; i++)
        {
            var offset = direction * barrierComponent.StepDistance * i;

            barrierComponent.PendingSegments.Add((
                _timing.CurTime + barrierComponent.SpawnDelayPerStep * i,
                new MapCoordinates(originCoords.Position + offset, originCoords.MapId)));

            barrierComponent.PendingSegments.Add((
                _timing.CurTime + barrierComponent.SpawnDelayPerStep * i,
                new MapCoordinates(originCoords.Position - offset, originCoords.MapId)));
        }
    }

    private void OnOrientationToggle(BarrierOrientationToggleEvent args)
    {
        var action = GetEntity(args.Action);
        var component = EnsureComp<BarrierOrientationComponent>(action);

        component.IsVertical = !component.IsVertical;
        Dirty(action, component);
    }

    private bool CanSpawn(EntityUid uid, BarrierComponent component, MapCoordinates coords)
    {
        var intersects = _lookupSystem.GetEntitiesInRange(coords, component.LookupRadius, LookupFlags.Static);

        foreach (var ent in intersects)
        {
            if (!HasComp<PhysicsComponent>(ent))
                continue;

            var (layer, _) = _physicsSystem.GetHardCollision(ent);
            if (layer != 0)
                return false;
        }

        return true;
    }
}
