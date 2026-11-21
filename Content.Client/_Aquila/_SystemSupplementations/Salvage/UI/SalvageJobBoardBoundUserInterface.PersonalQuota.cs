using Content.Shared.Salvage.JobBoard;

namespace Content.Client.Salvage.UI;

public sealed partial class SalvageJobBoardBoundUserInterface
{
    private HashSet<string> _mine = new();
    private SalvageJobBoardConsoleState? _state;

    private void AquilaBindClaim(SalvageJobBoardMenu menu)
    {
        menu.OnClaimButtonPressed += id =>
        {
            SendMessage(new JobBoardClaimMessage(id));
        };
    }

    private void AquilaStoreState(SalvageJobBoardConsoleState state)
    {
        _state = state;
        AquilaUpdateMenu();
    }

    private void AquilaUpdateMenu()
    {
        if (_menu is not { } menu || _state is not { } state)
            return;

        menu.Update(state, _mine);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is not JobBoardMineJobsMessage mine)
            return;

        _mine = mine.MineJobs;
        AquilaUpdateMenu();
    }
}
