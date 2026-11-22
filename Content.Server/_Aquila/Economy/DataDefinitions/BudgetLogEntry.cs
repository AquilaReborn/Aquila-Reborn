using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.DataDefinitions;

/// <summary>
/// Одна операция по счёту бюджета станции.
/// </summary>
public sealed class BudgetLogEntry
{
    /// <summary>
    /// Когда произошла операция.
    /// </summary>
    [ViewVariables]
    public TimeSpan Time;

    /// <summary>
    /// Счёт бюджета, по которому прошла операция.
    /// </summary>
    [ViewVariables]
    public ProtoId<CargoAccountPrototype> Account;

    /// <summary>
    /// Изменение баланса: положительное - зачисление, отрицательное - списание.
    /// </summary>
    [ViewVariables]
    public int Delta;

    /// <summary>
    /// Причина операции в истории.
    /// </summary>
    [ViewVariables]
    public LocId Reason;

    /// <summary>
    /// Вторая сторона операции: получатель, плательщик или инициатор, если известна.
    /// </summary>
    [ViewVariables]
    public string? Source;

    /// <summary>
    /// Пояснение к операции, например назначение платежа.
    /// </summary>
    [ViewVariables]
    public string? Comment;

    /// <summary>
    /// Баланс счёта после операции.
    /// </summary>
    [ViewVariables]
    public int BalanceAfter;
}
