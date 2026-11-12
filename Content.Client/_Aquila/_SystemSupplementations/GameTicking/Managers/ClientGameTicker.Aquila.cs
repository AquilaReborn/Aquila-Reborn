using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._Aquila.CCVars;
using Content.Shared.Chat;
using Robust.Client;
using Robust.Client.GameStates;
using Robust.Client.Replays.Playback;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Client.GameTicking.Managers;

public sealed partial class ClientGameTicker
{
    [Dependency] private readonly IBaseClient _autoReplayClient = default!;
    [Dependency] private readonly IClientGameStateManager _autoReplayGameStates = default!;
    [Dependency] private readonly IConfigurationManager _autoReplayConfig = default!;
    [Dependency] private readonly IResourceManager _autoReplayResources = default!;
    [Dependency] private readonly IReplayPlaybackManager _replayPlayback = default!;
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;

    private static readonly LocId AutoReplayBusy = "aquila-auto-replay-recording-busy";
    private static readonly LocId AutoReplayUnavailable = "aquila-auto-replay-recording-unavailable";
    private static readonly LocId AutoReplayStartFailed = "aquila-auto-replay-recording-start-failed";

    private readonly object _autoReplayOwner = new();

    private bool IsLiveReplaySession =>
        _autoReplayClient.RunLevel == ClientRunLevel.InGame && _replayPlayback.Replay == null;

    private void InitializeAutoReplay()
    {
        _autoReplayGameStates.GameStateApplied += OnAutoReplayGameStateApplied;
    }

    private void ShutdownAutoReplay()
    {
        _autoReplayGameStates.GameStateApplied -= OnAutoReplayGameStateApplied;
    }

    private AutoReplayStateComponent? GetAutoReplayState()
    {
        var query = AllEntityQuery<AutoReplayStateComponent>();
        return query.MoveNext(out _, out var state) ? state : null;
    }

    private void UpdateAutoReplayRecording(bool roundStarted, TimeSpan roundStartTime)
    {
        if (!IsLiveReplaySession)
            return;

        var state = GetAutoReplayState() ?? AddComp<AutoReplayStateComponent>(Spawn(null, MapCoordinates.Nullspace));
        if (!roundStarted)
        {
            state.SawPreRoundLobby = true;
            QueueAutoReplayStop();
            return;
        }

        var newRound = RoundStartTimeSpan != TimeSpan.Zero && roundStartTime > RoundStartTimeSpan;
        if (!state.SawPreRoundLobby && !newRound)
            return;

        if (newRound)
            QueueAutoReplayStop();

        state.SawPreRoundLobby = false;
        state.StartPending = _autoReplayConfig.GetCVar(AquilaCVars.AutoRecordReplay);
    }

    /// <summary>
    /// Запускает отложенную запись после обработки сетевого состояния и остановки записи предыдущего раунда.
    /// </summary>
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!IsLiveReplaySession || GetAutoReplayState() is not { StartPending: true, StopPending: false } state)
            return;

        state.StartPending = false;
        if (!_autoReplayConfig.GetCVar(AquilaCVars.AutoRecordReplay))
            return;

        if (_replayRecording.IsRecording)
        {
            ReportAutoReplayStartFailure(AutoReplayBusy);
            return;
        }

        if (!_replayRecording.CanStartRecording())
        {
            ReportAutoReplayStartFailure(AutoReplayUnavailable);
            return;
        }

        try
        {
            if (_replayRecording.TryStartRecording(_autoReplayResources.UserData, state: _autoReplayOwner))
                return;

            Log.Warning("The replay recorder declined the automatic start request.");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to start automatic replay recording: {e}");
            StopAutoReplayRecording();
        }

        ReportAutoReplayStartFailure(AutoReplayStartFailed);
    }

    private void ReportAutoReplayStartFailure(LocId reason)
    {
        var text = Loc.GetString(reason);
        var message = new ChatMessage(ChatChannel.Server, text, FormattedMessage.EscapeText(text), NetEntity.Invalid, null);
        _userInterfaceManager.GetUIController<ChatUIController>().ProcessChatMessage(message, speechBubble: false);
    }

    private void QueueAutoReplayStop()
    {
        if (!IsLiveReplaySession || GetAutoReplayState() is not { } state)
            return;

        state.StartPending = false;
        state.StopPending = ReferenceEquals(_replayRecording.ActiveRecordingState, _autoReplayOwner);
    }

    private void OnAutoReplayGameStateApplied(GameStateAppliedArgs args)
    {
        if (!IsLiveReplaySession || GetAutoReplayState() is not { StopPending: true } state)
            return;

        state.StopPending = false;
        StopAutoReplayRecording();
    }

    private void StopAutoReplayRecording()
    {
        if (!ReferenceEquals(_replayRecording.ActiveRecordingState, _autoReplayOwner))
            return;

        try
        {
            _replayRecording.StopRecording();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to stop automatic replay recording: {e}");
        }
    }
}
