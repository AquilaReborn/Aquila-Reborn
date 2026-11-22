using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

/// <summary>
/// Запись журнала бюджета, подготовленная для отображения на клиенте.
/// </summary>
[Serializable, NetSerializable]
public sealed class BudgetConsoleEntry(
    TimeSpan time,
    string account,
    int delta,
    string reason,
    string? source,
    string? comment,
    int balanceAfter)
{
    public readonly TimeSpan Time = time;
    public readonly string Account = account;
    public readonly int Delta = delta;
    public readonly string Reason = reason;
    public readonly string? Source = source;
    public readonly string? Comment = comment;
    public readonly int BalanceAfter = balanceAfter;
}
