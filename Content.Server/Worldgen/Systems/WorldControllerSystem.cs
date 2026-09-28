// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Server.Worldgen.Components;
using Content.Shared.CCVar; // Aquila Change
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration; // Aquila Change
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Worldgen.Systems;

/// <summary>
///     This handles putting together chunk entities and notifying them about important changes.
/// </summary>
public sealed class WorldControllerSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _xformSys = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!; // Aquila Change

    private const int PlayerLoadRadius = 2;

    private ISawmill _sawmill = default!;

    // Aquila Change start
    private float _loadBudget;

    private readonly List<(EntityUid Map, Vector2i Chunk, List<EntityUid> Loaders, int Priority)> _pendingChunks = new();
    // Aquila Change end

    /// <inheritdoc />
    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("world");
        Subs.CVar(_cfg, CCVars.WorldgenChunkLoadBudget, value => _loadBudget = value, true); // Aquila Change
        SubscribeLocalEvent<LoadedChunkComponent, ComponentStartup>(OnChunkLoadedCore);
        SubscribeLocalEvent<LoadedChunkComponent, ComponentShutdown>(OnChunkUnloadedCore);
        SubscribeLocalEvent<WorldChunkComponent, ComponentShutdown>(OnChunkShutdown);
    }

    /// <summary>
    ///     Handles deleting chunks properly.
    /// </summary>
    private void OnChunkShutdown(EntityUid uid, WorldChunkComponent component, ComponentShutdown args)
    {
        if (!TryComp<WorldControllerComponent>(component.Map, out var controller))
            return;

        if (HasComp<LoadedChunkComponent>(uid))
        {
            var ev = new WorldChunkUnloadedEvent(uid, component.Coordinates);
            RaiseLocalEvent(component.Map, ref ev);
            RaiseLocalEvent(uid, ref ev, broadcast: true);
        }

        controller.Chunks.Remove(component.Coordinates);
    }

    /// <summary>
    ///     Handles the inner logic of loading a chunk, i.e. events.
    /// </summary>
    private void OnChunkLoadedCore(EntityUid uid, LoadedChunkComponent component, ComponentStartup args)
    {
        if (!TryComp<WorldChunkComponent>(uid, out var chunk))
            return;

        var ev = new WorldChunkLoadedEvent(uid, chunk.Coordinates);
        RaiseLocalEvent(chunk.Map, ref ev);
        RaiseLocalEvent(uid, ref ev, broadcast: true);
        //_sawmill.Debug($"Loaded chunk {ToPrettyString(uid)} at {chunk.Coordinates}");
    }

    /// <summary>
    ///     Handles the inner logic of unloading a chunk, i.e. events.
    /// </summary>
    private void OnChunkUnloadedCore(EntityUid uid, LoadedChunkComponent component, ComponentShutdown args)
    {
        if (!TryComp<WorldChunkComponent>(uid, out var chunk))
            return;

        if (Terminating(uid))
            return; // SAFETY: This is in case a loaded chunk gets deleted, to avoid double unload.

        var ev = new WorldChunkUnloadedEvent(uid, chunk.Coordinates);
        RaiseLocalEvent(chunk.Map, ref ev);
        RaiseLocalEvent(uid, ref ev);
        //_sawmill.Debug($"Unloaded chunk {ToPrettyString(uid)} at {coords}");
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        //there was a to-do here about every frame alloc but it turns out it's a nothing burger here.
        var chunksToLoad = new Dictionary<EntityUid, Dictionary<Vector2i, List<EntityUid>>>();
        var chunkPriority = new Dictionary<EntityUid, Dictionary<Vector2i, int>>(); // Aquila Change

        var controllerEnum = EntityQueryEnumerator<WorldControllerComponent>();
        while (controllerEnum.MoveNext(out var uid, out _))
        {
            chunksToLoad[uid] = new Dictionary<Vector2i, List<EntityUid>>();
            chunkPriority[uid] = new Dictionary<Vector2i, int>(); // Aquila Change
        }

        if (chunksToLoad.Count == 0)
            return; // Just bail early.

        var loaderEnum = EntityQueryEnumerator<WorldLoaderComponent, TransformComponent>();

        while (loaderEnum.MoveNext(out var uid, out var worldLoader, out var xform))
        {
            var mapOrNull = xform.MapUid;
            if (mapOrNull is null)
                continue;
            var map = mapOrNull.Value;
            if (!chunksToLoad.ContainsKey(map))
                continue;

            var wc = _xformSys.GetWorldPosition(xform);
            var coords = WorldGen.WorldToChunkCoords(wc);
            var center = coords.Floored(); // Aquila Change
            var chunks = new GridPointsNearEnumerator(center, // Aquila Change
                (int) Math.Ceiling(worldLoader.Radius / (float) WorldGen.ChunkSize) + 1);

            var set = chunksToLoad[map];
            var priorities = chunkPriority[map]; // Aquila Change

            while (chunks.MoveNext(out var chunk))
            {
                // Aquila Change start
                var delta = chunk.Value - center;
                AddLoader(set, priorities, chunk.Value, uid, delta.X * delta.X + delta.Y * delta.Y);
                // Aquila Change end
            }
        }

        var mindEnum = EntityQueryEnumerator<MindContainerComponent, TransformComponent>();
        var ghostQuery = GetEntityQuery<GhostComponent>();

        // Mindful entities get special privilege as they're always a player and we don't want the illusion being broken around them.
        while (mindEnum.MoveNext(out var uid, out var mind, out var xform))
        {
            if (!mind.HasMind)
                continue;
            if (ghostQuery.HasComponent(uid))
                continue;
            var mapOrNull = xform.MapUid;
            if (mapOrNull is null)
                continue;
            var map = mapOrNull.Value;
            if (!chunksToLoad.ContainsKey(map))
                continue;

            var wc = _xformSys.GetWorldPosition(xform);
            var coords = WorldGen.WorldToChunkCoords(wc);
            var chunks = new GridPointsNearEnumerator(coords.Floored(), PlayerLoadRadius);

            var set = chunksToLoad[map];
            var priorities = chunkPriority[map]; // Aquila Change

            while (chunks.MoveNext(out var chunk))
            {
                AddLoader(set, priorities, chunk.Value, uid, -1); // Aquila Change
            }
        }

        var loadedEnum = EntityQueryEnumerator<LoadedChunkComponent, WorldChunkComponent>();
        var chunksUnloaded = 0;

        // Make sure these chunks get unloaded at the end of the tick.
        while (loadedEnum.MoveNext(out var uid, out var _, out var chunk))
        {
            var coords = chunk.Coordinates;

            if (!chunksToLoad[chunk.Map].ContainsKey(coords))
            {
                RemCompDeferred<LoadedChunkComponent>(uid);
                chunksUnloaded++;
            }
        }

        if (chunksUnloaded > 0)
            _sawmill.Debug($"Queued {chunksUnloaded} chunks for unload.");

        if (chunksToLoad.All(x => x.Value.Count == 0))
            return;

        var startTime = _gameTiming.RealTime;
        var count = 0;
        var loadedQuery = GetEntityQuery<LoadedChunkComponent>();
        var controllerQuery = GetEntityQuery<WorldControllerComponent>();

        // Aquila Change start
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
        // Aquila Change end
    }

    // Aquila Change start
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
    // Aquila Change end

    /// <summary>
    ///     Attempts to get a chunk, creating it if it doesn't exist.
    /// </summary>
    /// <param name="chunk">Chunk coordinates to get the chunk entity for.</param>
    /// <param name="map">Map the chunk is in.</param>
    /// <param name="controller">The controller this chunk belongs to.</param>
    /// <returns>A chunk, if available.</returns>
    [Pure]
    public EntityUid? GetOrCreateChunk(Vector2i chunk, EntityUid map, WorldControllerComponent? controller = null)
    {
        if (!Resolve(map, ref controller))
            throw new Exception($"Tried to use {ToPrettyString(map)} as a world map, without actually being one.");

        if (controller.Chunks.TryGetValue(chunk, out var ent))
            return ent;
        return CreateChunkEntity(chunk, map, controller);
    }

    /// <summary>
    ///     Constructs a new chunk entity, attaching it to the map.
    /// </summary>
    /// <param name="chunkCoords">The coordinates the new chunk should be initialized for.</param>
    /// <param name="map"></param>
    /// <param name="controller"></param>
    /// <returns></returns>
    private EntityUid CreateChunkEntity(Vector2i chunkCoords, EntityUid map, WorldControllerComponent controller)
    {
        var chunk = Spawn(controller.ChunkProto, MapCoordinates.Nullspace);
        StartupChunkEntity(chunk, chunkCoords, map, controller);
        _metaData.SetEntityName(chunk, $"Chunk {chunkCoords.X}/{chunkCoords.Y}");
        return chunk;
    }

    private void StartupChunkEntity(EntityUid chunk, Vector2i coords, EntityUid map,
        WorldControllerComponent controller)
    {
        if (!TryComp<WorldChunkComponent>(chunk, out var chunkComponent))
        {
            _sawmill.Error($"Chunk {ToPrettyString(chunk)} is missing WorldChunkComponent.");
            return;
        }

        ref var chunks = ref controller.Chunks;

        chunks[coords] = chunk; // Add this entity to chunk index.
        chunkComponent.Coordinates = coords;
        chunkComponent.Map = map;
        var ev = new WorldChunkAddedEvent(chunk, coords);
        RaiseLocalEvent(map, ref ev, broadcast: true);
    }
}

/// <summary>
///     A directed event fired when a chunk is initially set up in the world. The chunk is not loaded at this point.
/// </summary>
[ByRefEvent]
[PublicAPI]
public readonly record struct WorldChunkAddedEvent(EntityUid Chunk, Vector2i Coords);

/// <summary>
///     A directed event fired when a chunk is loaded into the world, i.e. a player or other world loader has entered vicinity.
/// </summary>
[ByRefEvent]
[PublicAPI]
public readonly record struct WorldChunkLoadedEvent(EntityUid Chunk, Vector2i Coords);

/// <summary>
///     A directed event fired when a chunk is unloaded from the world, i.e. no world loaders remain nearby.
/// </summary>
[ByRefEvent]
[PublicAPI]
public readonly record struct WorldChunkUnloadedEvent(EntityUid Chunk, Vector2i Coords);