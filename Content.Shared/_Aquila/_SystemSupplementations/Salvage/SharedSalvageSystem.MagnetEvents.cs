using System.Linq;
using Content.Shared._Aquila.Salvage.Magnet;
using Content.Shared.Salvage.Magnet;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageSystem
{
    private void ApplyAsteroidEvent(ref AsteroidOffering offering, System.Random random)
    {
        var asteroidEvent = PickAsteroidEvent(random);

        if (asteroidEvent == null)
            return;

        if (asteroidEvent.Layers.Count == 0 && asteroidEvent.Duration == null)
            return;

        offering.DungeonConfig.Layers.AddRange(asteroidEvent.Layers);
        offering.Event = asteroidEvent.ID;
        offering.Duration = asteroidEvent.Duration;
    }

    private AsteroidEventPrototype? PickAsteroidEvent(System.Random random)
    {
        var events = _proto.EnumeratePrototypes<AsteroidEventPrototype>().ToList();
        events.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));

        var total = 0f;

        foreach (var asteroidEvent in events)
            total += asteroidEvent.Weight;

        if (total <= 0f)
            return null;

        var roll = (float) random.NextDouble() * total;

        foreach (var asteroidEvent in events)
        {
            roll -= asteroidEvent.Weight;

            if (roll < 0f)
                return asteroidEvent;
        }

        return null;
    }
}
