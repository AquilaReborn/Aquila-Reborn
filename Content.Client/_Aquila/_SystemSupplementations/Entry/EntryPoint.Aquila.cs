using Content.Client.Replay;
using Robust.Client;
using Robust.Client.Replays.Loading;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Replays;

namespace Content.Client.Entry;

public sealed partial class EntryPoint
{
    [Dependency] private readonly ContentReplayPlaybackManager _playbackMan = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IReplayLoadManager _replayLoad = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private void InitializeReplayPlayback()
    {
        _playbackMan.Initialize();
    }

    private bool SkipReplayDisconnectState(ClientRunLevel oldLevel)
    {
        if (oldLevel == ClientRunLevel.SinglePlayerGame)
            return _playbackMan.IsReplayLoadInProgress || _playbackMan.IsLocalLibraryReplay;

        return _playbackMan.IsLocalLibraryReplay && _playbackMan.IsReplayLoadInProgress;
    }

    private bool LoadBundleReplay()
    {
        var folder = ReplayConstants.ReplayZipFolder.ToRootedPath();
        if (!_configManager.GetCVar(CVars.LaunchContentBundle) ||
            !_resourceManager.ContentFileExists(folder / ReplayConstants.FileMeta))
            return false;

        if (_playbackMan.IsReplayLoadInProgress)
            return true;

        _logManager.GetSawmill("entry").Info("Loading content bundle replay from VFS!");
        _playbackMan.LastLoad = (null, folder);
        _replayLoad.LoadAndStartReplay(new ReplayFileReaderResources(_resourceManager, folder));
        return true;
    }
}
