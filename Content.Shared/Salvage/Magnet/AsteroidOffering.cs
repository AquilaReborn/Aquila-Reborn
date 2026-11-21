// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Procedural;

namespace Content.Shared.Salvage.Magnet;

#pragma warning disable CS0282 // Aquila Change
/// <summary>
/// Asteroid offered for the magnet.
/// </summary>
public partial record struct AsteroidOffering : ISalvageMagnetOffering // Aquila Change
{
    public string Id;

    public DungeonConfig DungeonConfig;

    /// <summary>
    /// Calculated marker layers for the asteroid.
    /// </summary>
    public Dictionary<string, int> MarkerLayers;

    uint ISalvageMagnetOffering.Cost => 0; // DeltaV
}
#pragma warning restore CS0282 // Aquila Change