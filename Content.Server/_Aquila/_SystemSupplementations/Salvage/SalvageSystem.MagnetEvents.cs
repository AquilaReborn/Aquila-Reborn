using Content.Server.Salvage.Magnet;
using Content.Shared.Salvage.Magnet;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private static TimeSpan? GetAsteroidEventDuration(ISalvageMagnetOffering offering)
    {
        return offering is AsteroidOffering asteroidOffering ? asteroidOffering.Duration : null;
    }

    private static TimeSpan GetMagnetActiveDuration(SalvageMagnetDataComponent data)
    {
        return data.ActiveDuration ?? data.ActiveTime;
    }
}
