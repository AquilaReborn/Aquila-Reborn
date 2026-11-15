using Content.Server.Worldgen.Components;
using Robust.Shared.Configuration;

namespace Content.Server.Worldgen.Systems;

public sealed partial class WorldControllerSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _loadBudget;

    private readonly List<(EntityUid Map, Vector2i Chunk, List<EntityUid> Loaders, int Priority)> _pendingChunks = new();

    private void LoadChunksByPriority(
        Dictionary<EntityUid, Dictionary<Vector2i, List<EntityUid>>> chunksToLoad,
        Dictionary<EntityUid, Dictionary<Vector2i, int>> chunkPriority,
        TimeSpan startTime,
        EntityQuery<LoadedChunkComponent> loadedQuery,
        EntityQuery<WorldControllerComponent> controllerQuery)
    {
        var count = 0;

        _pendingChunks.Clear();
        foreach (var (map, chunks) in chunksToLoad)
        {
            var controller = controllerQuery.GetComponent(map);
            var priorities = chunkPriority[map];
            foreach (var (chunk, loaders) in chunks)
            {
                if (controller.Chunks.TryGetValue(chunk, out var existing)
                    && loadedQuery.TryGetComponent(existing, out var loaded))
                {
                    loaded.Loaders = loaders;
                    continue;
                }

                _pendingChunks.Add((map, chunk, loaders, priorities[chunk]));
            }
        }

        if (_pendingChunks.Count == 0)
            return;

        _pendingChunks.Sort(static (a, b) => a.Priority.CompareTo(b.Priority));
        var budget = _loadBudget > 0 ? TimeSpan.FromMilliseconds(_loadBudget) : TimeSpan.MaxValue;

        foreach (var (map, chunk, loaders, priority) in _pendingChunks)
        {
            if (priority >= 0 && _gameTiming.RealTime - startTime > budget)
                break;

            var controller = controllerQuery.GetComponent(map);
            var ent = GetOrCreateChunk(chunk, map, controller); // Ensure everything loads.
            if (ent is null)
                continue;

            if (!loadedQuery.TryGetComponent(ent.Value, out var c))
            {
                c = AddComp<LoadedChunkComponent>(ent.Value);
                count += 1;
            }

            c.Loaders = loaders;
        }

        if (count > 0)
        {
            var timeSpan = _gameTiming.RealTime - startTime;
            var remaining = _pendingChunks.Count - count;
            _sawmill.Debug(remaining > 0
                ? $"Loaded {count} chunks in {timeSpan.TotalMilliseconds:N2}ms, {remaining} left for the next ticks."
                : $"Loaded {count} chunks in {timeSpan.TotalMilliseconds:N2}ms.");
        }
    }

    private static void AddLoader(
        Dictionary<Vector2i, List<EntityUid>> set,
        Dictionary<Vector2i, int> priorities,
        Vector2i chunk,
        EntityUid loader,
        int priority)
    {
        if (!set.TryGetValue(chunk, out var loaders))
        {
            loaders = new List<EntityUid>(4);
            set[chunk] = loaders;
            priorities[chunk] = priority;
        }
        else if (priority < priorities[chunk])
        {
            priorities[chunk] = priority;
        }

        loaders.Add(loader);
    }
}
