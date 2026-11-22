using Content.Shared._Aquila.Economy;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.Economy;

public sealed class BudgetConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BudgetConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BudgetConsoleWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is BudgetConsoleUiState consoleState)
            _window?.UpdateState(consoleState);
    }
}
