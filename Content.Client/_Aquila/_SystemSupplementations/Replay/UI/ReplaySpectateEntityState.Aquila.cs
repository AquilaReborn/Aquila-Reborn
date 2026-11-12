using Robust.Client.Replays.UI;

namespace Content.Client.Replay.UI;

public partial class ReplaySpectateEntityState
{
    private static readonly LocId ReturnToLibrary = "aquila-replay-library-return";

    private void ConfigureLocalReplayControls(ReplayControlWidget widget)
    {
        if (!_replayManager.IsLocalLibraryReplay)
            return;

        widget.StopButton.Text = Loc.GetString(ReturnToLibrary);
    }
}
