using Content.Server._DV.CosmicCult.Components;
using Robust.Shared.EntitySerialization;

namespace Content.Server._DV.CosmicCult;

public sealed partial class CosmicCultSystem
{
    /// <summary>
    /// Загружает карту космической пустоты, если она ещё не загружена.
    /// </summary>
    public void EnsureCosmicVoid()
    {
        var query = EntityQueryEnumerator<CosmicVoidSpawnComponent>();
        if (query.MoveNext(out _, out _))
            return;

        if (_mapLoader.TryLoadMap(_mapPath, out var map, out _, new DeserializationOptions { InitializeMaps = true }))
            _map.SetPaused(map.Value.Comp.MapId, false);
    }
}
