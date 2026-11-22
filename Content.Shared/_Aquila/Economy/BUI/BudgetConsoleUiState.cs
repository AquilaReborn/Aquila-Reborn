using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

/// <summary>
/// Состояние окна консоли с журналом операций бюджета станции.
/// </summary>
[Serializable, NetSerializable]
public sealed class BudgetConsoleUiState(List<BudgetConsoleEntry> entries) : BoundUserInterfaceState
{
    public readonly List<BudgetConsoleEntry> Entries = entries;
}
