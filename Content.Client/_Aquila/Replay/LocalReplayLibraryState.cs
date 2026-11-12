using Content.Client.Replay;
using Robust.Client.State;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.Replay;

/// <summary>
/// Экран библиотеки локальных реплеев.
/// </summary>
public sealed class LocalReplayLibraryState : State
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly ContentReplayPlaybackManager _playback = default!;

    protected override void Startup()
    {
        _ui.GetUIController<LocalReplayLibraryUIController>().Open();
    }

    protected override void Shutdown()
    {
        _ui.GetUIController<LocalReplayLibraryUIController>().Close();
        _playback.LeaveLocalReplayLibrary();
    }
}
