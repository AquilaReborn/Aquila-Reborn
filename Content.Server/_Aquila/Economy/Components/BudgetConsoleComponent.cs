using Content.Server._Aquila.Economy.Systems;

namespace Content.Server._Aquila.Economy.Components;

/// <summary>
/// Консоль с журналом операций бюджета станции.
/// </summary>
[RegisterComponent, Access(typeof(BudgetConsoleSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class BudgetConsoleComponent : Component
{
    /// <summary>
    /// Сколько последних записей журнала показывать в консоли.
    /// </summary>
    [DataField]
    public int MaxEntries = 200;

    /// <summary>
    /// Когда окно консоли можно обновить следующим событием.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextUpdate;
}
