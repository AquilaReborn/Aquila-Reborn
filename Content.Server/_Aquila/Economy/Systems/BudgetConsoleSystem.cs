using Content.Server._Aquila.Economy.Components;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.Economy.BUI;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

/// <summary>
/// Наполняет окно консоли журналом операций бюджета станции.
/// </summary>
public sealed class BudgetConsoleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<BudgetConsoleComponent>(BudgetConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
        });
    }

    /// <summary>
    /// Обновляет открытые консоли станции, если с прошлого обновления прошло достаточно времени.
    /// Если времени прошло мало, консоль запоминает, что её нужно обновить, и обновление не теряется.
    /// </summary>
    /// <param name="station">Станция, чей журнал бюджета изменился.</param>
    public void Refresh(EntityUid station)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BudgetConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (_station.GetOwningStation(uid) != station
                || !_ui.IsUiOpen(uid, BudgetConsoleUiKey.Key))
                continue;

            if (now < console.NextUpdate)
            {
                console.UpdatePending = true;
                continue;
            }

            console.NextUpdate = now + UpdateInterval;
            console.UpdatePending = false;
            UpdateUiState((uid, console));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BudgetConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (!console.UpdatePending || now < console.NextUpdate)
                continue;

            console.UpdatePending = false;
            console.NextUpdate = now + UpdateInterval;

            if (_ui.IsUiOpen(uid, BudgetConsoleUiKey.Key))
                UpdateUiState((uid, console));
        }
    }

    private void OnBoundUIOpened(Entity<BudgetConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<BudgetConsoleComponent> ent)
    {
        var entries = new List<BudgetConsoleEntry>();

        var station = _station.GetOwningStation(ent.Owner);
        if (station != null && TryComp<StationBudgetLogComponent>(station, out var log))
        {
            var first = Math.Max(0, log.Entries.Count - ent.Comp.MaxEntries);
            for (var i = log.Entries.Count - 1; i >= first; i--)
            {
                var entry = log.Entries[i];
                entries.Add(new BudgetConsoleEntry(
                    entry.Time,
                    entry.Account.Id,
                    entry.Delta,
                    entry.Reason.Id,
                    entry.Source,
                    entry.Comment,
                    entry.BalanceAfter));
            }
        }

        _ui.SetUiState(ent.Owner, BudgetConsoleUiKey.Key, new BudgetConsoleUiState(entries));
    }
}
