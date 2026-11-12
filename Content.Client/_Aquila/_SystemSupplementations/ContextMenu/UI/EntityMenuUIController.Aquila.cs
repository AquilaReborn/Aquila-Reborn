using Robust.Client.Replays.Playback;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client.ContextMenu.UI;

public sealed partial class EntityMenuUIController
{
    [Dependency] private readonly INetManager _contextConnection = default!;
    [Dependency] private readonly IReplayPlaybackManager _contextReplay = default!;

    private bool CanSendContextInputAquila()
    {
        return _contextConnection.IsConnected && _contextReplay.Replay == null;
    }

    private bool IsCombatContextBlockedAquila(ICommonSession? session)
    {
        return _contextReplay.Replay == null && _combatMode.IsInCombatMode(session?.AttachedEntity);
    }

    internal bool HandleOfflineContextInputAquila(ICommonSession? session, ClientFullInputCmdMessage message)
    {
        if (!_updating || !message.Coordinates.IsValid(_entityManager))
            return false;

        return HandleOpenEntityMenu(new PointerInputCmdHandler.PointerInputCmdArgs(session, message.Coordinates,
            message.ScreenCoordinates, message.Uid, message.State, message));
    }
}
