using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Kitchen;

/// <summary>
/// Ключи данных внешнего вида мясорубки.
/// </summary>
[Serializable, NetSerializable]
public enum MeatGrinderVisuals : byte
{
    /// <summary>
    /// bool: машина сейчас перемалывает труп.
    /// </summary>
    IsWorking,

    /// <summary>
    /// bool: машина уже запачкана кровью.
    /// </summary>
    IsBloody
}

/// <summary>
/// Слои спрайта мясорубки.
/// </summary>
[Serializable, NetSerializable]
public enum MeatGrinderVisualLayers : byte
{
    /// <summary>
    /// Корпус: grinder в покое и grinder_on во время работы.
    /// </summary>
    Base,

    /// <summary>
    /// Слой крови поверх корпуса.
    /// </summary>
    Blood
}
