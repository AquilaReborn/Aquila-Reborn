using Content.Server._Aquila.Economy.Components;
using Content.Server._Aquila.Economy.DataDefinitions;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

/// <summary>
/// Ведёт журнал операций по счетам бюджета станции.
/// </summary>
public sealed class StationBudgetLogSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BudgetConsoleSystem _budgetConsole = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;

    private static readonly LocId ExternalReason = "bank-budget-log-external";
    private static readonly LocId DistributionReason = "bank-budget-log-distribution";
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(0.5);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationBankAccountComponent, BankBalanceUpdatedEvent>(OnBankBalanceUpdated);
    }

    /// <summary>
    /// Записывает операцию по счёту бюджета станции. Вызывать после изменения бюджета: баланс после операции
    /// берётся из бюджета в момент записи.
    /// </summary>
    /// <param name="station">Станция, чей бюджет изменился.</param>
    /// <param name="account">Счёт бюджета, по которому прошла операция.</param>
    /// <param name="delta">Изменение баланса: положительное - зачисление, отрицательное - списание.</param>
    /// <param name="reason">Причина операции в истории.</param>
    /// <param name="source">Вторая сторона операции, если известна.</param>
    /// <param name="comment">Пояснение к операции, например назначение платежа.</param>
    public void LogTransaction(
        EntityUid station,
        ProtoId<CargoAccountPrototype> account,
        int delta,
        LocId reason,
        string? source = null,
        string? comment = null)
    {
        if (delta == 0 || !TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        var log = EnsureComp<StationBudgetLogComponent>(station);
        var balance = _cargo.GetBalanceFromAccount((station, bank), account);
        var now = _timing.CurTime;
        log.LastBalances[account] = balance;

        if (log.Entries.Count > 0)
        {
            var last = log.Entries[^1];
            if (last.Reason == ExternalReason
                && last.Account == account
                && last.Delta == delta
                && now - last.Time <= DuplicateWindow)
            {
                last.Reason = reason;
                last.Source = source;
                last.Comment = comment;
                _budgetConsole.Refresh(station);
                return;
            }
        }

        AddEntry(log, account, delta, reason, source, comment, balance, now);
        _budgetConsole.Refresh(station);
    }

    private void OnBankBalanceUpdated(Entity<StationBankAccountComponent> ent, ref BankBalanceUpdatedEvent args)
    {
        var log = EnsureComp<StationBudgetLogComponent>(ent);
        var now = _timing.CurTime;

        List<(ProtoId<CargoAccountPrototype> Account, int Delta, int Balance)>? changed = null;

        foreach (var (account, balance) in args.Balance)
        {
            if (!log.LastBalances.TryGetValue(account, out var previous))
            {
                log.LastBalances[account] = balance;
                continue;
            }

            var delta = balance - previous;
            log.LastBalances[account] = balance;

            if (delta == 0)
                continue;

            changed ??= new List<(ProtoId<CargoAccountPrototype> Account, int Delta, int Balance)>();
            changed.Add((account, delta, balance));
        }

        if (changed is null || IsStationIncome(ent.Comp, changed))
            return;

        var reason = changed.Count == 1 ? ExternalReason : DistributionReason;
        foreach (var (account, delta, balance) in changed)
            AddEntry(log, account, delta, reason, null, null, balance, now);

        _budgetConsole.Refresh(ent);
    }

    private static bool IsStationIncome(
        StationBankAccountComponent bank,
        List<(ProtoId<CargoAccountPrototype> Account, int Delta, int Balance)> changed)
    {
        var income = (int) Math.Round(bank.IncreasePerSecond * bank.IncomeDelay.TotalSeconds);
        if (income <= 0)
            return false;

        IReadOnlyDictionary<ProtoId<CargoAccountPrototype>, double> distribution = bank.RevenueDistribution;

        foreach (var (account, delta, _) in changed)
        {
            if (!distribution.TryGetValue(account, out var percent)
                || delta != (int) Math.Round(percent * income))
                return false;
        }

        return true;
    }

    private void AddEntry(
        StationBudgetLogComponent log,
        ProtoId<CargoAccountPrototype> account,
        int delta,
        LocId reason,
        string? source,
        string? comment,
        int balance,
        TimeSpan now)
    {
        log.Entries.Add(new BudgetLogEntry
        {
            Time = now,
            Account = account,
            Delta = delta,
            Reason = reason,
            Source = source,
            Comment = comment,
            BalanceAfter = balance,
        });

        if (log.Entries.Count > log.MaxEntries)
            log.Entries.RemoveRange(0, log.Entries.Count - log.MaxEntries);
    }
}
