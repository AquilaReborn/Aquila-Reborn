using Content.Shared.Salvage.Magnet;

namespace Content.Client.Salvage.UI;

public sealed partial class SalvageMagnetBoundUserInterface
{
    private static readonly LocId DungeonConfigLocPrefix = "dungeon-config-proto-";
    private static readonly LocId AsteroidEventLocPrefix = "asteroid-event-proto-";
    private static readonly LocId AsteroidEventTitle = "salvage-magnet-asteroid-event";

    private string GetAsteroidOfferingTitle(AsteroidOffering asteroid)
    {
        var asteroidName = Loc.GetString($"{DungeonConfigLocPrefix}{asteroid.Id}");

        if (asteroid.Event is not { } asteroidEvent)
            return asteroidName;

        var eventName = Loc.GetString($"{AsteroidEventLocPrefix}{asteroidEvent.Id}");
        return Loc.GetString(AsteroidEventTitle, ("asteroid", asteroidName), ("event", eventName));
    }
}
