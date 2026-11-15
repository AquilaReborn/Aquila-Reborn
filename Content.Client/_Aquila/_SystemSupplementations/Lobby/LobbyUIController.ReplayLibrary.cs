using Content.Client._Aquila.Replay;
using Content.Client.Lobby.UI;
using Content.Shared._Aquila.CCVars;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby;

public sealed partial class LobbyUIController
{
    private LobbyGui? _replayLobby;

    private void BindReplayControls(LobbyGui? lobby)
    {
        if (lobby == null)
            return;

        _replayLobby = lobby;
        lobby.ReplayLibraryButton.OnPressed += OpenLobbyReplayLibrary;
        lobby.AutoRecordReplay.Pressed = _configurationManager.GetCVar(AquilaCVars.AutoRecordReplay);
        lobby.AutoRecordReplay.OnToggled += OnAutoRecordReplayToggled;
    }

    private void UnbindReplayControls()
    {
        if (_replayLobby is not { } lobby)
            return;

        _replayLobby = null;
        lobby.ReplayLibraryButton.OnPressed -= OpenLobbyReplayLibrary;
        lobby.AutoRecordReplay.OnToggled -= OnAutoRecordReplayToggled;
        UIManager.GetUIController<LocalReplayLibraryUIController>().CloseFromLobby();
    }

    private void OpenLobbyReplayLibrary(BaseButton.ButtonEventArgs args)
    {
        UIManager.GetUIController<LocalReplayLibraryUIController>().Open(fromLobby: true);
    }

    private void OnAutoRecordReplayToggled(BaseButton.ButtonToggledEventArgs args)
    {
        _configurationManager.SetCVar(AquilaCVars.AutoRecordReplay, args.Pressed);
        _configurationManager.SaveToFile();
    }
}
