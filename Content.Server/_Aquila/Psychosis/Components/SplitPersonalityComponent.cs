namespace Content.Server._Aquila.Psychosis.Components;

/// <summary>
/// Тело с раздвоением личности. Вторая личность сидит в сущности-пассажире внутри тела.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SplitPersonalityComponent : Component
{
    /// <summary>
    /// Сущность, в которой находится личность, не управляющая телом.
    /// </summary>
    [ViewVariables]
    public EntityUid? Passenger;

    /// <summary>
    /// Разум, которому тело принадлежало изначально.
    /// </summary>
    [ViewVariables]
    public EntityUid? OriginalMind;

    /// <summary>
    /// Разум второй личности, появляется, когда роль займёт призрак.
    /// </summary>
    [ViewVariables]
    public EntityUid? AlterMind;

    /// <summary>
    /// Действие смены личности, выданное телу.
    /// </summary>
    [ViewVariables]
    public EntityUid? SwapAction;

    /// <summary>
    /// Сущность, которая попросила поменяться местами и ждёт ответа.
    /// </summary>
    [ViewVariables]
    public EntityUid? PendingRequester;

    /// <summary>
    /// Добавили ли мы телу коллективный разум сами, чтобы убрать его при излечении.
    /// </summary>
    [ViewVariables]
    public bool AddedCollectiveMind;

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
    /// До какого момента роль второй личности должен занять призрак, иначе раздвоение проходит само.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan ClaimDeadline;

    /// <summary>
    /// Когда в следующий раз проверять лекарства в крови.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextCheck;
}
