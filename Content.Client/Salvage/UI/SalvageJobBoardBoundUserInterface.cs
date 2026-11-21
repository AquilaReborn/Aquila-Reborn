using Content.Shared.Cargo.Components;
using Content.Shared.Salvage.JobBoard;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Salvage.UI;

[UsedImplicitly]
public sealed partial class SalvageJobBoardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey) // Aquila Change
{
    [ViewVariables]
    private SalvageJobBoardMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SalvageJobBoardMenu>();

        _menu.OnLabelButtonPressed += id =>
        {
            SendMessage(new JobBoardPrintLabelMessage(id));
        };

        AquilaBindClaim(_menu); // Aquila Change
    }

    protected override void UpdateState(BoundUserInterfaceState message)
    {
        base.UpdateState(message);

        if (message is not SalvageJobBoardConsoleState state)
            return;

        AquilaStoreState(state); // Aquila Change
    }
}
