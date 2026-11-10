using Content.Server.Atmos.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Robust.Shared.Map.Components;

namespace Content.Server._Aquila.Doors;

/// <summary>
/// Разворачивает двери с <see cref="DoorAutoOrientComponent"/> по соседним стенам при MapInit,
/// чтобы направленный спрайт совпадал со стеной. Ось выбирается по числу соседей: стены, окна
/// и такие же двери в ряду считаются опорой, другие двери не учитываются, при равенстве поворот не меняется.
/// Стыковочные шлюзы и краевые двери не трогает.
/// </summary>
public sealed class DoorAutoOrientSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<AirtightComponent> _airtightQuery;
    private EntityQuery<DoorAutoOrientComponent> _autoOrientQuery;

    public override void Initialize()
    {
        base.Initialize();

        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _autoOrientQuery = GetEntityQuery<DoorAutoOrientComponent>();

        SubscribeLocalEvent<DoorAutoOrientComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<DoorAutoOrientComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.Enabled || HasComp<DockingComponent>(ent))
            return;

        if (_airtightQuery.TryComp(ent, out var ownAirtight) && !IsFullTile(ownAirtight))
            return;

        var xform = Transform(ent);
        if (!xform.Anchored || xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var proto = Prototype(ent)?.ID;
        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var horizontal = CountSupports(proto, gridUid, grid, tile, Direction.West, Direction.East);
        var vertical = CountSupports(proto, gridUid, grid, tile, Direction.South, Direction.North);

        if (horizontal == vertical)
            return;

        var facesSide = xform.LocalRotation.GetCardinalDir() is Direction.East or Direction.West;

        if (horizontal > vertical && facesSide)
            _transform.SetLocalRotation(ent, Direction.South.ToAngle(), xform);
        else if (vertical > horizontal && !facesSide)
            _transform.SetLocalRotation(ent, Direction.East.ToAngle(), xform);
    }

    private static bool IsFullTile(AirtightComponent airtight)
    {
        return airtight.InitialAirBlockedDirection == (int) AtmosDirection.All;
    }

    private int CountSupports(string? proto, EntityUid gridUid, MapGridComponent grid, Vector2i tile, Direction first, Direction second)
    {
        var count = 0;
        if (IsSupport(proto, gridUid, grid, tile + first.ToIntVec()))
            count++;

        if (IsSupport(proto, gridUid, grid, tile + second.ToIntVec()))
            count++;

        return count;
    }

    private bool IsSupport(string? proto, EntityUid gridUid, MapGridComponent grid, Vector2i indices)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, indices);
        while (anchored.MoveNext(out var uid))
        {
            if (_autoOrientQuery.HasComp(uid.Value))
            {
                if (proto != null && Prototype(uid.Value)?.ID == proto)
                    return true;

                continue;
            }

            if (_airtightQuery.TryComp(uid.Value, out var airtight)
                && airtight.InitialAirBlockedDirection != (int) AtmosDirection.Invalid)
                return true;
        }

        return false;
    }
}
