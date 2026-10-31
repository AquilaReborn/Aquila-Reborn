namespace Content.Server._Aquila.Psychosis.Components;

/// <summary>
/// Тот, кому мерещится воображаемый друг.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ImaginaryFriendOwnerComponent : Component
{
    /// <summary>
    /// Воображаемый друг.
    /// </summary>
    [ViewVariables]
    public EntityUid? Friend;

    /// <summary>
    /// Сколько секунд лечения уже прошло.
    /// </summary>
    [ViewVariables]
    public float CureProgress;

    /// <summary>
    /// Сколько секунд нужно держать лекарство в крови для излечения.
    /// </summary>
    [DataField]
    public float CureTime = 60f;

    /// <summary>
    /// До какого момента роль друга должен занять призрак, иначе друг исчезает сам.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan ClaimDeadline;

    /// <summary>
    /// Когда в следующий раз проверять лекарства и расстояние.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextCheck;
}
