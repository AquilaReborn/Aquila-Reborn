using Content.Client._Aquila.Replay;
using Robust.Client;
using Robust.Client.State;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.MainMenu;

public sealed partial class MainScreen
{
    [Dependency] private readonly IStateManager _replayLibraryStateManager = default!;

    private void InitializeReplayLibrary()
    {
        _mainMenuControl.ReplayLibraryButton.OnPressed += OpenReplayLibrary;
    }

    private void ShutdownReplayLibrary()
    {
        _mainMenuControl.ReplayLibraryButton.OnPressed -= OpenReplayLibrary;
    }

    private void OpenReplayLibrary(BaseButton.ButtonEventArgs args)
    {
        if (!_isConnecting && _client.RunLevel == ClientRunLevel.Initialize)
            _replayLibraryStateManager.RequestStateChange<LocalReplayLibraryState>();
    }
}
