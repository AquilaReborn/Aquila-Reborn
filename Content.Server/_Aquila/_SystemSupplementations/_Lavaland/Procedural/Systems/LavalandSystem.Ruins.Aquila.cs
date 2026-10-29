using Content.Shared.Decals;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Lavaland.Procedural.Systems;

public sealed partial class LavalandSystem
{
    private void CopyRuinDecals(
        Entity<MapGridComponent> grid,
        EntityUid lavaland,
        Vector2i offset,
        HashSet<Vector2i>? reservedTiles,
        DecalGridComponent loadedDecals)
    {
        foreach (var (_, decal) in _decals.GetDecalsIntersecting(grid.Owner, grid.Comp.LocalAABB, loadedDecals))
        {
            var position = decal.Coordinates + offset;

            if (reservedTiles?.Contains(position.Floored()) == true)
                continue;

            _decals.TryAddDecal(decal.WithCoordinates(position), new EntityCoordinates(lavaland, position), out _);
        }
    }
}
