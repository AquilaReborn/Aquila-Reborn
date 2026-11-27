namespace Content.Shared._Aquila.Heretic.Lock.Components;

/// <summary>
/// Барьер лабиринта: не пропускает никого, кроме еретиков и их слуг.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class LabyrinthBarrierComponent : Component
{
    /// <summary>
    /// Сила, с которой барьер отбрасывает не-еретиков.
    /// </summary>
    [DataField]
    public float ThrowForce = 12f;

    /// <summary>
    /// Сообщение, которое видит отброшенное существо.
    /// </summary>
    [DataField]
    public LocId RepelMessage = "heretic-lock-barrier-repel";
}
