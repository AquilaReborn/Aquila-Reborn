using Robust.Client.GameObjects;
using Robust.Client.Replays.Playback;
using Robust.Shared.Input;
using Robust.Shared.Player;

namespace Content.Client.Gameplay;

public partial class GameplayStateBase
{
    [Dependency] private readonly IReplayPlaybackManager _replayInputPlayback = default!;

    private bool HandleViewportInputAquila(InputSystem input, ICommonSession? session, BoundKeyFunction function,
        ClientFullInputCmdMessage message)
    {
        if (_replayInputPlayback.Replay == null)
            return input.HandleInputCommand(session, function, message);

        foreach (var handler in input.BindRegistry.GetHandlers(function))
        {
            if (handler.FireOutsidePrediction && handler.HandleCmdMessage(_entityManager, session, message))
                return true;
        }

        return false;
    }
}
