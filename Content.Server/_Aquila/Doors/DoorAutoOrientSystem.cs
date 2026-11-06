using Content.Server.Atmos.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Atmos;
using Robust.Shared.Map.Components;

namespace Content.Server._Aquila.Doors;

/// <summary>
/// Разворачивает двери с <see cref="DoorAutoOrientComponent"/> по соседним стенам при MapInit,
/// чтобы направленный спрайт совпадал со стеной. Стыковочные шлюзы и краевые двери не трогает.
/// </summary>
public sealed class DoorAutoOrientSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<AirtightComponent> _airtightQuery;

    public override void Initialize()
    {
        base.Initialize();

        _airtightQuery = GetEntityQuery<AirtightComponent>();

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

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var horizontal = IsSolid(gridUid, grid, tile + Direction.West.ToIntVec())
                         && IsSolid(gridUid, grid, tile + Direction.East.ToIntVec());
        var vertical = IsSolid(gridUid, grid, tile + Direction.South.ToIntVec())
                       && IsSolid(gridUid, grid, tile + Direction.North.ToIntVec());

        if (horizontal == vertical)
            return;

        var facesSide = xform.LocalRotation.GetCardinalDir() is Direction.East or Direction.West;

        if (horizontal && facesSide)
            _transform.SetLocalRotation(ent, Direction.South.ToAngle(), xform);
        else if (vertical && !facesSide)
            _transform.SetLocalRotation(ent, Direction.East.ToAngle(), xform);
    }

    private static bool IsFullTile(AirtightComponent airtight)
    {
        return airtight.InitialAirBlockedDirection == (int) AtmosDirection.All;
    }

    private bool IsSolid(EntityUid gridUid, MapGridComponent grid, Vector2i indices)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, indices);
        while (anchored.MoveNext(out var uid))
        {
            if (_airtightQuery.TryComp(uid, out var airtight) && IsFullTile(airtight))
                return true;
        }

        return false;
    }
}
