namespace Content.Shared.Heretic;

public sealed partial class EventHereticLastRefugee
{
    /// <summary>
    /// Радиус, в котором живые разумные существа не дают уйти в убежище.
    /// </summary>
    [DataField]
    public float SearchRadius = 5f;
}
