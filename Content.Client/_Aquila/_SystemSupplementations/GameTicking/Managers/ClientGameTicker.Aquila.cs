using Content.Shared._Aquila.CCVars;
using Robust.Client;
using Robust.Client.Replays.Playback;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Replays;

namespace Content.Client.GameTicking.Managers;

public sealed partial class ClientGameTicker
{
    [Dependency] private readonly IBaseClient _autoReplayClient = default!;
    [Dependency] private readonly IConfigurationManager _autoReplayConfig = default!;
    [Dependency] private readonly IResourceManager _autoReplayResources = default!;
    [Dependency] private readonly IReplayPlaybackManager _replayPlayback = default!;
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;

    private readonly object _autoReplayOwner = new();
    private bool _sawPreRoundLobby;
    private bool _autoReplayStartPending;

    private void UpdateAutoReplayRecording(bool roundStarted)
    {
        if (_autoReplayClient.RunLevel != ClientRunLevel.InGame || _replayPlayback.Replay != null)
            return;

        if (!roundStarted)
        {
            _sawPreRoundLobby = true;
            StopAutoReplayRecording();
            return;
        }

        if (!_sawPreRoundLobby)
            return;

        _sawPreRoundLobby = false;
        _autoReplayStartPending = _autoReplayConfig.GetCVar(AquilaCVars.AutoRecordReplay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_autoReplayStartPending)
            return;

        _autoReplayStartPending = false;
        if (_autoReplayClient.RunLevel != ClientRunLevel.InGame || _replayPlayback.Replay != null ||
            !_autoReplayConfig.GetCVar(AquilaCVars.AutoRecordReplay) || !_replayRecording.CanStartRecording())
            return;

        try
        {
            _replayRecording.TryStartRecording(_autoReplayResources.UserData, state: _autoReplayOwner);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to start automatic replay recording: {e}");
        }
    }

    private void StopAutoReplayRecording()
    {
        _autoReplayStartPending = false;

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
