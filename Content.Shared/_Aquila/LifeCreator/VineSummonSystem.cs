using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.LifeCreator;

public sealed class VineSummonSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<(TimeSpan Time, EntityCoordinates Coords, EntProtoId Proto)> _pending = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VineSummonActionEvent>(OnSummonAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var curTime = _timing.CurTime;

        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            var entry = _pending[i];

            if (entry.Time > curTime)
                continue;

            _pending.RemoveAt(i);
            Spawn(entry.Proto, entry.Coords);
        }
    }

    private void OnSummonAction(VineSummonActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_net.IsServer)
            return;

        var center = args.Target;

        if (_transform.GetGrid(center) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var centerTile = _map.TileIndicesFor(grid, gridComp, center);
        var radius = args.Radius;
        var curTime = _timing.CurTime;

        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                if (x * x + y * y > radius * radius)
                    continue;

                var tile = new Vector2i(centerTile.X + x, centerTile.Y + y);
                var coords = _map.GridTileToLocal(grid, gridComp, tile);

                var distance = MathF.Sqrt(x * x + y * y);
                var delay = args.SpawnDelayPerTile * distance;

                _pending.Add((curTime + delay, coords, args.VinePrototype));
            }
        }
    }
}
