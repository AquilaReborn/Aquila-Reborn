using System.IO;
using Content.Client._Aquila.Replay;
using Content.Client.Replay.UI.Loading;
using Robust.Client;
using Robust.Client.Replays.Loading;
using Robust.Client.Upload;
using Robust.Shared.Prototypes;

namespace Content.Client.Replay;

public sealed partial class ContentReplayPlaybackManager
{
    [Dependency] private readonly NetworkResourceManager _replayResources = default!;
    [Dependency] private readonly IPrototypeManager _replayPrototypes = default!;

    internal bool IsLocalLibraryReplay => DefaultState == typeof(LocalReplayLibraryState);

    internal void LoadLocalReplay(IReplayFileReader reader)
    {
        var previousDefault = DefaultState;
        var previousLoad = LastLoad;
        var previousScreenshotMode = IsScreenshotMode;
        DefaultState = typeof(LocalReplayLibraryState);
        LastLoad = null;
        IsScreenshotMode = false;

        try
        {
            LoadOverride(reader);
        }
        catch
        {
            DefaultState = previousDefault;
            LastLoad = previousLoad;
            IsScreenshotMode = previousScreenshotMode;
            throw;
        }
    }

    internal void LeaveLocalReplayLibrary()
    {
        if (IsLocalLibraryReplay)
            DefaultState = null;
    }

    private void BindReplayLoadCompletion(LoadingScreen<bool> screen, IReplayFileReader reader)
    {
        if (IsLocalLibraryReplay)
            screen.OnJobFinished += (_, error) => OnLocalReplayLoadFinished(reader, error);
        else
            screen.OnJobFinished += (_, error) => OnFinishedLoading(error);
    }

    private void OnLocalReplayLoadFinished(IReplayFileReader reader, Exception? error)
    {
        if (error == null && _playback.Replay != null)
        {
            OnFinishedLoading(null);
            return;
        }

        if (_playback.Replay != null)
        {
            _playback.StopReplay();
        }
        else
        {
            reader.Dispose();

            if (_client.RunLevel == ClientRunLevel.SinglePlayerGame)
            {
                _client.StopSinglePlayer();
                _replayResources.ClearResources();
                _replayPrototypes.Reset();
            }
        }

        OnFinishedLoading(error ?? new InvalidDataException(Loc.GetString(LocalReplayLibraryUIController.LoadError)));
    }
}
