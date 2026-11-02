namespace Content.Server._Aquila.Psychosis.Components;

/// <summary>
/// Тот, кому мерещатся воображаемые друзья или кто носит в себе энграммы.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(ImaginaryFriendSystem))]
public sealed partial class ImaginaryFriendOwnerComponent : Component
{
    /// <summary>
    /// Все спутники этого существа. Они видят и слышат друг друга, потому что смотрят его глазами.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Friends = new();

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
    /// Когда в следующий раз проверять лекарства и расстояние.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextCheck;
}
