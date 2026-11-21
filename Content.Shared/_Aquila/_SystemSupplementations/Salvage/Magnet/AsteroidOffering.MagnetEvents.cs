using Content.Shared._Aquila.Salvage.Magnet;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Magnet;

public partial record struct AsteroidOffering
{
    /// <summary>
    /// Выпавшее событие астероида, если оно есть.
    /// </summary>
    public ProtoId<AsteroidEventPrototype>? Event;

    /// <summary>
    /// Время удержания астероида у магнита, заданное событием, если оно есть.
    /// </summary>
    public TimeSpan? Duration;
}
