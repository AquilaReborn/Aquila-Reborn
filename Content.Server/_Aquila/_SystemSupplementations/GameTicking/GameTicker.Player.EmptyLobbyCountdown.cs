using System.Linq;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    private void ResetCountdownIfLobbyEmpty(ICommonSession leaving)
    {
        if (LobbyEnabled
            && RunLevel == GameRunLevel.PreRoundLobby
            && _playerManager.Sessions.All(s => s == leaving || s.Status == SessionStatus.Disconnected))
        {
            _roundStartCountdownHasNotStartedYetDueToNoPlayers = true;
        }
    }
}
