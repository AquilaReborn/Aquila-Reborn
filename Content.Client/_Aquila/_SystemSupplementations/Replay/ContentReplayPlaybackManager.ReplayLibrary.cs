using System.IO;
using Content.Client._Aquila.Replay;
using Content.Client.Replay.UI.Loading;
using Content.Shared.GameTicking;
using Robust.Client;
using Robust.Client.Replays.Loading;
using Robust.Client.State;
using Robust.Client.Upload;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Replay;

public sealed partial class ContentReplayPlaybackManager
{
    [Dependency] private readonly NetworkResourceManager _replayResources = default!;
    [Dependency] private readonly IPrototypeManager _replayPrototypes = default!;
    [Dependency] private readonly ILogManager _replayLog = default!;

    private static readonly LocId DisconnectReason = "aquila-replay-library-disconnect-reason";
    private IReplayFileReader? _loadingReader;

    internal bool IsLocalLibraryReplay => DefaultState == typeof(LocalReplayLibraryState);
    internal bool IsReplayLoadInProgress => _loadingReader != null;
    internal bool CanStartReplayLoad => !IsReplayLoadInProgress && _playback.Replay == null;

    internal void LoadLocalReplay(IReplayFileReader reader, bool disconnect)
    {
        if (!CanStartReplayLoad)
            throw new InvalidOperationException(Loc.GetString(LocalReplayLibraryUIController.LoadError));

        var previousDefault = DefaultState;
        var previousLoad = LastLoad;
        var previousScreenshotMode = IsScreenshotMode;
        _loadingReader = reader;
        DefaultState = typeof(LocalReplayLibraryState);
        LastLoad = null;
        IsScreenshotMode = false;

        try
        {
            if (disconnect)
                _client.DisconnectFromServer(Loc.GetString(DisconnectReason));

            if (_client.RunLevel != ClientRunLevel.Initialize)
                throw new InvalidOperationException(Loc.GetString(LocalReplayLibraryUIController.LoadError));

            StartReplayLoad(reader);
        }
        catch
        {
            _loadingReader = null;
            DefaultState = previousDefault;
            LastLoad = previousLoad;
            IsScreenshotMode = previousScreenshotMode;
            throw;
        }
    }

    internal void LeaveLocalReplayLibrary()
    {
        if (IsLocalLibraryReplay && !IsReplayLoadInProgress)
            DefaultState = null;
    }

    private static bool HandleRecordedLobbyTransition(object message)
    {
        return message is TickerJoinLobbyEvent;
    }

    private void LoadReplayWithGuard(IReplayFileReader reader)
    {
        if (!CanStartReplayLoad)
        {
            if (!ReferenceEquals(reader, _loadingReader))
                reader.Dispose();

            _replayLog.GetSawmill("replay").Warning("Ignoring overlapping replay load.");
            return;
        }

        _loadingReader = reader;
        try
        {
            StartReplayLoad(reader);
        }
        catch (Exception error)
        {
            _loadingReader = null;
            reader.Dispose();
            OnFinishedLoading(error);
        }
    }

    private void StartReplayLoad(IReplayFileReader reader)
    {
        var lastLoad = LastLoad;
        var screen = _stateMan.RequestStateChange<LoadingScreen<bool>>();
        var job = new ContentLoadReplayJob(1 / 60f, reader, _loadMan, screen);
        screen.Job = job;
        screen.OnJobFinished += Finish;
        _stateMan.OnStateChanged += OnLoadingStateChanged;
        _controller.TickUpdateOverride += BlockReplayLoadingTick;

        void Detach()
        {
            screen.OnJobFinished -= Finish;
            _stateMan.OnStateChanged -= OnLoadingStateChanged;
            _controller.TickUpdateOverride -= BlockReplayLoadingTick;
        }

        void Finish(bool result, Exception? error)
        {
            Detach();
            try
            {
                if (error != null || !result || _playback.Replay == null)
                {
                    CleanUpFailedReplayLoad(reader);
                    error ??= new InvalidDataException(Loc.GetString(LocalReplayLibraryUIController.LoadError));
                }
            }
            finally
            {
                LastLoad = lastLoad;
                _loadingReader = null;
            }

            OnFinishedLoading(error);
        }

        void OnLoadingStateChanged(StateChangedEventArgs args)
        {
            if (args.OldState != screen || _playback.Replay != null)
                return;

            Detach();
            try
            {
                if (job.Status == JobStatus.Pending)
                {
                    reader.Dispose();
                }
                else
                {
                    job.CancelReplayLoad();
                    CleanUpFailedReplayLoad(reader);
                }
            }
            finally
            {
                _loadingReader = null;
                LastLoad = null;
                if (_stateMan.CurrentState is LocalReplayLibraryState)
                    _uiMan.GetUIController<LocalReplayLibraryUIController>().UpdateActions();
                else
                    LeaveLocalReplayLibrary();
            }
        }
    }

    private void CleanUpFailedReplayLoad(IReplayFileReader reader)
    {
        var replayStarted = _playback.Replay != null;
        if (replayStarted)
            _playback.StopReplay();
        else
            reader.Dispose();

        if (_client.RunLevel == ClientRunLevel.SinglePlayerGame)
            _client.StopSinglePlayer();

        if (!replayStarted)
        {
            _replayResources.ClearResources();
            _replayPrototypes.Reset();
        }

        _timing.Paused = false;
    }

    private static void BlockReplayLoadingTick(FrameEventArgs args)
    {
    }
}
