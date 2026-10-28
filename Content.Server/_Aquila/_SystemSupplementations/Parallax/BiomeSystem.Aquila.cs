using System.Numerics;
using System.Threading.Tasks;
using Content.Server._Lavaland.Biome;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem
{
    private Dictionary<Vector2i, string?[]>? CreateMarkerEntityCache(
        BiomeComponent component,
        Entity<MapGridComponent>? grid,
        Dictionary<string, HashSet<Vector2i>> markers,
        BiomeOptimizeComponent? optimize)
    {
        HashSet<Vector2i>? origins = null;

        foreach (var (layer, chunks) in markers)
        {
            var size = ProtoManager.Index<BiomeMarkerLayerPrototype>(layer).Size;
            component.LoadedMarkers.TryGetValue(layer, out var loaded);

            foreach (var chunk in chunks)
            {
                if ((loaded != null && loaded.Contains(chunk)) || IsOutsideLoadArea(chunk, size, optimize))
                    continue;

                origins ??= new HashSet<Vector2i>();
                var min = SharedMapSystem.GetChunkIndices(chunk, ChunkSize);
                var max = SharedMapSystem.GetChunkIndices(chunk + new Vector2i(size - 1, size - 1), ChunkSize);

                for (var x = min.X; x <= max.X; x++)
                {
                    for (var y = min.Y; y <= max.Y; y++)
                    {
                        origins.Add(new Vector2i(x, y) * ChunkSize);
                    }
                }
            }
        }

        if (origins == null)
            return null;

        var cache = new Dictionary<Vector2i, string?[]>(origins.Count);
        var entries = new List<(Vector2i Origin, string?[] Entities)>(origins.Count);

        foreach (var origin in origins)
        {
            var entities = new string?[ChunkSize * ChunkSize];
            cache.Add(origin, entities);
            entries.Add((origin, entities));
        }

        Parallel.ForEach(entries, new ParallelOptions() { MaxDegreeOfParallelism = _parallel.ParallelProcessCount }, entry =>
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    TryGetEntity(entry.Origin + new Vector2i(x, y), component, grid, out var entity);
                    entry.Entities[x + y * ChunkSize] = entity;
                }
            }
        });

        return cache;
    }

    private string? GetMarkerEntity(
        Vector2i node,
        BiomeComponent biome,
        Entity<MapGridComponent>? grid,
        Dictionary<Vector2i, string?[]>? entityCache)
    {
        var origin = SharedMapSystem.GetChunkIndices(node, ChunkSize) * ChunkSize;

        if (entityCache != null && entityCache.TryGetValue(origin, out var entities))
            return entities[node.X - origin.X + (node.Y - origin.Y) * ChunkSize];

        TryGetEntity(node, biome, grid, out var entity);
        return entity;
    }

    private static bool IsOutsideLoadArea(Vector2i chunk, int size, BiomeOptimizeComponent? optimize)
    {
        if (optimize == null)
            return false;

        var area = optimize.LoadArea;
        return chunk.X >= area.Right
               || chunk.Y >= area.Top
               || chunk.X + size <= area.Left
               || chunk.Y + size <= area.Bottom;
    }
}
