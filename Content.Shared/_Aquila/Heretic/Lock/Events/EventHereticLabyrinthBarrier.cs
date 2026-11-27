using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Heretic.Lock.Events;

/// <summary>
/// Ставит барьер лабиринта в выбранной точке: сквозь него проходят только еретики и их слуги.
/// </summary>
public sealed partial class EventHereticLabyrinthBarrier : WorldTargetActionEvent
{
    private static readonly EntProtoId DefaultBarrier = "HereticLabyrinthBarrier";

    /// <summary>
    /// Прототип барьера, который появляется в выбранной точке.
    /// </summary>
    [DataField]
    public EntProtoId Barrier = DefaultBarrier;
}
