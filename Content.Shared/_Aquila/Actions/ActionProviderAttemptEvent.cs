namespace Content.Shared._Aquila.Actions;

/// <summary>
/// Поднимается на сущности, которая выдала действие (например, импланте), перед его использованием.
/// Позволяет запретить действия отключённого или заблокированного устройства.
/// </summary>
[ByRefEvent]
public record struct ActionProviderAttemptEvent(EntityUid User)
{
    /// <summary>
    /// Запрещено ли использовать действие.
    /// </summary>
    public bool Cancelled;
}
