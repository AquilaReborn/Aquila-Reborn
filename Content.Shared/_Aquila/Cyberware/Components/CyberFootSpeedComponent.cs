namespace Content.Shared._Aquila.Cyberware.Components;

/// <summary>
/// Кибернетическая ступня, которая немного ускоряет носителя, пока пришита к телу.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class CyberFootSpeedComponent : Component
{
    /// <summary>
    /// Во сколько раз эта ступня умножает скорость ходьбы.
    /// </summary>
    [DataField]
    public float WalkModifier = 1.05f;

    /// <summary>
    /// Во сколько раз эта ступня умножает скорость бега.
    /// </summary>
    [DataField]
    public float SprintModifier = 1.05f;
}
