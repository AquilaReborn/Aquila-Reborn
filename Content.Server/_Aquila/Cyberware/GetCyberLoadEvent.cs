namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Поднимается на каждом импланте, органе и части тела, чтобы собрать их кибернагрузку.
/// </summary>
[ByRefEvent]
public record struct GetCyberLoadEvent
{
    /// <summary>
    /// Накопленная нагрузка.
    /// </summary>
    public float Load;
}
