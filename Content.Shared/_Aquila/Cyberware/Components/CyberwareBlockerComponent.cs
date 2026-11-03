namespace Content.Shared._Aquila.Cyberware.Components;

/// <summary>
/// Ошейник-блокиратор: пока надет на шею, глушит все импланты носителя и снижает кибернагрузку.
/// </summary>
[RegisterComponent]
public sealed partial class CyberwareBlockerComponent : Component
{
    /// <summary>
    /// Во сколько раз снижается кибернагрузка носителя, пока ошейник надет.
    /// </summary>
    [DataField]
    public float LoadMultiplier = 0.5f;
}
