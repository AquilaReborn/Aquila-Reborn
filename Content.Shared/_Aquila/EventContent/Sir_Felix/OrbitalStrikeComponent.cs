using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.EventContent.Sir_Felix;

/// <summary>
/// Предмет, при использовании в руке запускающий серию орбитальных ударов вокруг пользователя.
/// </summary>
[RegisterComponent]
public sealed partial class OrbitalStrikeComponent : Component
{
    [DataField]
    public EntProtoId PodPrototype = "OrbitalStrikePodSpawn";

    /// <summary>
    /// Интервал между появлением капсул.
    /// </summary>
    [DataField]
    public TimeSpan SpawnInterval = TimeSpan.FromSeconds(1.25);

    /// <summary>
    /// Время от появления капсулы до взрыва.
    /// </summary>
    [DataField]
    public TimeSpan ExplosionDelay = TimeSpan.FromSeconds(3.5);

    [DataField]
    public List<int> PodCounts = new() { 3, 6, 8, 15, 20, 25, 30, 40, 60, 100 };

    [DataField]
    public int CurrentPodCount = 6;

    [DataField]
    public List<float> Radii = new() { 15f, 20f, 30f, 40f, 50f, 60f, 100f };

    [DataField]
    public float CurrentRadius = 30f;

    [DataField]
    public List<OrbitalExplosionMode> Modes = new()
    {
        new OrbitalExplosionMode { Name = "orbital-strike-mode-weak", Intensity = 70f, Slope = 1f, MaxTileIntensity = 7f },
        new OrbitalExplosionMode { Name = "orbital-strike-mode-medium", Intensity = 120f, Slope = 1f, MaxTileIntensity = 12f },
        new OrbitalExplosionMode { Name = "orbital-strike-mode-strong", Intensity = 160f, Slope = 3f, MaxTileIntensity = 100f },
    };

    /// <summary>
    /// Индекс выбранного режима в <see cref="Modes"/>.
    /// </summary>
    [DataField]
    public int CurrentMode = 1;
}
