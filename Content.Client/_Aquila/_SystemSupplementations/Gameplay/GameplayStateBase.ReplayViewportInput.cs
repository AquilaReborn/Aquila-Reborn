using Content.Client.ContextMenu.UI;
using Content.Shared.Input;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Replays.Playback;
using Robust.Shared.Input;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client.Gameplay;

public partial class GameplayStateBase
{
    [Dependency] private readonly IReplayPlaybackManager _replayInputPlayback = default!;
    [Dependency] private readonly INetManager _viewportConnection = default!;

    private bool HandleViewportInputAquila(InputSystem input, ICommonSession? session, BoundKeyFunction function,
        ClientFullInputCmdMessage message)
    {
        if (_viewportConnection.IsConnected && _replayInputPlayback.Replay == null)
            return input.HandleInputCommand(session, function, message);

        if (function == EngineKeyFunctions.UseSecondary)
            return UserInterfaceManager.GetUIController<EntityMenuUIController>()
                .HandleOfflineContextInputAquila(session, message);

        if (function == EngineKeyFunctions.CameraRotateLeft ||
            function == EngineKeyFunctions.CameraRotateRight ||
            function == EngineKeyFunctions.CameraReset)
        {
            if (message.State == BoundKeyState.Up && _playerManager.LocalEntity is { } player)
            {
                var mover = _entitySystemManager.GetEntitySystem<SharedMoverController>();
                if (function == EngineKeyFunctions.CameraReset)
                    mover.ResetCamera(player);
                else
                    mover.RotateCamera(player, (function == EngineKeyFunctions.CameraRotateLeft
                        ? Direction.East
                        : Direction.West).ToAngle());
            }

            return true;
        }

        if (!IsOfflineViewportBindAquila(function))
            return false;

        foreach (var handler in input.BindRegistry.GetHandlers(function))
        {
            if (handler.FireOutsidePrediction && handler.HandleCmdMessage(_entityManager, session, message))
                return true;
        }

        return false;
    }

    private static bool IsOfflineViewportBindAquila(BoundKeyFunction function)
    {
        return function == EngineKeyFunctions.MoveUp ||
            function == EngineKeyFunctions.MoveDown ||
            function == EngineKeyFunctions.MoveLeft ||
            function == EngineKeyFunctions.MoveRight ||
            function == ContentKeyFunctions.ExamineEntity ||
            function == ContentKeyFunctions.InspectEntity ||
            function == ContentKeyFunctions.InspectClientComponent ||
            function == ContentKeyFunctions.NamePeek;
    }
}
