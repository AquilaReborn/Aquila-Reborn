using Content.Server._Aquila.Economy.DataDefinitions;
using Content.Server._Aquila.Economy.Systems;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.Components;

/// <summary>
/// Журнал операций по счетам бюджета станции.
/// </summary>
[RegisterComponent, Access(typeof(StationBudgetLogSystem))]
public sealed partial class StationBudgetLogComponent : Component
{
    /// <summary>
    /// Записи журнала от старых к новым.
    /// </summary>
    [ViewVariables]
    public List<BudgetLogEntry> Entries = new();

    /// <summary>
    /// Последние увиденные балансы счетов: по ним считается изменение, о котором сообщает событие бюджета.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<CargoAccountPrototype>, int> LastBalances = new();

    /// <summary>
    /// Сколько последних записей хранится в журнале. Более старые записи удаляются.
    /// </summary>
    [DataField]
    public int MaxEntries = 500;
}
