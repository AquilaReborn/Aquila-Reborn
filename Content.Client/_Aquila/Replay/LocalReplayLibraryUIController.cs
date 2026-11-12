using System.IO;
using System.IO.Compression;
using System.Linq;
using Content.Client.Lobby;
using Content.Client.Replay;
using Robust.Client;
using Robust.Client.Replays.Loading;
using Robust.Client.Replays.Playback;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.Replay;

public sealed class LocalReplayLibraryUIController : UIController
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly IFileDialogManager _fileDialog = default!;
    [Dependency] private readonly IReplayPlaybackManager _playback = default!;
    [Dependency] private readonly ContentReplayPlaybackManager _contentPlayback = default!;

    private const string ArchiveExtension = "zip";
    private static readonly FileDialogFilters ReplayFilter = new(new FileDialogFilters.Group(ArchiveExtension));
    private static readonly LocId Empty = "aquila-replay-library-empty";
    private static readonly LocId ListError = "aquila-replay-library-list-error";
    private static readonly LocId DisconnectReason = "aquila-replay-library-disconnect-reason";
    internal static readonly LocId LoadError = "aquila-replay-library-load-error";

    private LocalReplayLibraryWindow? _window;
    private ResPath? _selected;
    private ReplayFileReaderZip? _pendingReplay;
    private bool _fromLobby;
    private bool _picking;

    private bool IsConnectedLobby => _state.CurrentState is LobbyState &&
        _client.RunLevel is (ClientRunLevel.Connected or ClientRunLevel.InGame);

    private bool CanOpenReplay => _playback.Replay == null && (_fromLobby
        ? IsConnectedLobby
        : _state.CurrentState is LocalReplayLibraryState && _client.RunLevel == ClientRunLevel.Initialize);

    internal void Open(bool fromLobby = false)
    {
        if (fromLobby && !IsConnectedLobby)
            return;

        if (_window is { } openWindow && _fromLobby == fromLobby)
        {
            openWindow.MoveToFront();
            return;
        }

        Close();
        var window = new LocalReplayLibraryWindow();
        _window = window;
        _fromLobby = fromLobby;
        _client.RunLevelChanged += OnRunLevelChanged;
        window.OnClose += OnWindowClosed;
        window.ReplayList.OnItemSelected += OnReplaySelected;
        window.ReplayList.OnItemDeselected += OnReplayDeselected;
        window.PlayButton.OnPressed += PlaySelected;
        window.BrowseButton.OnPressed += Browse;
        window.RefreshButton.OnPressed += Refresh;
        window.ConfirmPlayButton.OnPressed += ConfirmPlay;
        window.CancelPlayButton.OnPressed += CancelPlay;
        Populate(window);
        UpdateActions(window);
        window.OpenCentered();
    }

    internal void CloseFromLobby()
    {
        if (_fromLobby)
            Close();
    }

    internal void Close()
    {
        if (_window is not { } window)
            return;

        _window = null;
        _selected = null;
        _pendingReplay?.Dispose();
        _pendingReplay = null;
        _client.RunLevelChanged -= OnRunLevelChanged;
        window.OnClose -= OnWindowClosed;
        window.ReplayList.OnItemSelected -= OnReplaySelected;
        window.ReplayList.OnItemDeselected -= OnReplayDeselected;
        window.PlayButton.OnPressed -= PlaySelected;
        window.BrowseButton.OnPressed -= Browse;
        window.RefreshButton.OnPressed -= Refresh;
        window.ConfirmPlayButton.OnPressed -= ConfirmPlay;
        window.CancelPlayButton.OnPressed -= CancelPlay;
        window.Close();
    }

    private void OnWindowClosed()
    {
        Close();
        if (_fromLobby)
            return;

        _contentPlayback.LeaveLocalReplayLibrary();
        _contentPlayback.ReturnToDefaultState();
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs args)
    {
        if (_window is { } window)
            UpdateActions(window);
    }

    private void Refresh(BaseButton.ButtonEventArgs args)
    {
        if (_window is { } window && _pendingReplay == null)
            Populate(window);
    }

    private void Populate(LocalReplayLibraryWindow window)
    {
        _selected = null;
        window.PlayButton.Disabled = true;
        window.ReplayList.Clear();
        window.StatusLabel.Visible = false;

        try
        {
            var directory = new ResPath(_config.GetCVar(CVars.ReplayDirectory)).ToRootedPath();
            var userData = _resources.UserData;
            var names = userData.DirectoryEntries(directory).OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
            var items = new List<ItemList.Item>();
            foreach (var name in names)
            {
                if (!name.EndsWith("." + ArchiveExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                var path = directory / name;
                if (userData.IsDir(path))
                    continue;

                items.Add(new ItemList.Item(window.ReplayList)
                {
                    Text = name,
                    TooltipText = name,
                    Metadata = path,
                });
            }

            window.ReplayList.Add(items);
            if (items.Count == 0)
                ShowStatus(window, Empty);
        }
        catch (Exception error)
        {
            ReportError(window, ListError, error);
        }
    }

    private void OnReplaySelected(ItemList.ItemListSelectedEventArgs args)
    {
        _selected = (ResPath?) args.ItemList[args.ItemIndex].Metadata;
        if (_window is { } window)
            UpdateActions(window);
    }

    private void OnReplayDeselected(ItemList.ItemListDeselectedEventArgs args)
    {
        _selected = null;
        if (_window is { } window)
            window.PlayButton.Disabled = true;
    }

    private void PlaySelected(BaseButton.ButtonEventArgs args)
    {
        if (_window is not { } window || _selected is not { } path || !CanOpenReplay ||
            _picking || _pendingReplay != null)
            return;

        try
        {
            OpenReplay(window, _resources.UserData.OpenRead(path));
        }
        catch (Exception error)
        {
            ReportError(window, LoadError, error);
        }
    }

    private async void Browse(BaseButton.ButtonEventArgs args)
    {
        if (_window is not { } window || !CanOpenReplay || _picking || _pendingReplay != null)
            return;

        _picking = true;
        UpdateActions(window);
        try
        {
            var stream = await _fileDialog.OpenFile(ReplayFilter, FileAccess.Read);
            if (stream == null)
                return;

            if (_window != window || !CanOpenReplay)
            {
                stream.Dispose();
                return;
            }

            OpenReplay(window, stream);
        }
        catch (Exception error)
        {
            ReportError(window, LoadError, error);
        }
        finally
        {
            _picking = false;
            if (_window is { } current)
                UpdateActions(current);
        }
    }

    private void OpenReplay(LocalReplayLibraryWindow window, Stream stream)
    {
        ReplayFileReaderZip? reader = null;
        try
        {
            reader = new ReplayFileReaderZip(new ZipArchive(stream), ReplayConstants.ReplayZipFolder);
            if (!reader.Exists(ReplayConstants.FileMeta))
                throw new InvalidDataException(Loc.GetString(LoadError));
        }
        catch
        {
            if (reader == null)
                stream.Dispose();
            else
                reader.Dispose();

            throw;
        }

        if (_fromLobby)
        {
            _pendingReplay = reader;
            window.StatusLabel.Visible = false;
            UpdateActions(window);
            return;
        }

        StartReplay(window, reader, disconnect: false);
    }

    private void ConfirmPlay(BaseButton.ButtonEventArgs args)
    {
        if (_window is not { } window || _pendingReplay is not { } reader || !CanOpenReplay)
            return;

        _pendingReplay = null;
        UpdateActions(window);
        StartReplay(window, reader, disconnect: true);
    }

    private void CancelPlay(BaseButton.ButtonEventArgs args)
    {
        _pendingReplay?.Dispose();
        _pendingReplay = null;
        if (_window is { } window)
            UpdateActions(window);
    }

    private void StartReplay(LocalReplayLibraryWindow window, ReplayFileReaderZip reader, bool disconnect)
    {
        try
        {
            if (disconnect)
                _client.DisconnectFromServer(Loc.GetString(DisconnectReason));

            if (_client.RunLevel != ClientRunLevel.Initialize || _playback.Replay != null)
                throw new InvalidOperationException(Loc.GetString(LoadError));

            _contentPlayback.LoadLocalReplay(reader);
        }
        catch (Exception error)
        {
            reader.Dispose();
            if (_window != window && _client.RunLevel == ClientRunLevel.Initialize && _playback.Replay == null)
                _state.RequestStateChange<LocalReplayLibraryState>();

            ReportError(_window ?? window, LoadError, error);
        }
    }

    private void UpdateActions(LocalReplayLibraryWindow window)
    {
        var confirming = _pendingReplay != null;
        window.DisconnectConfirmation.Visible = confirming;
        window.LibraryActions.Visible = !confirming;
        window.ConfirmPlayButton.Disabled = !CanOpenReplay;
        window.ReplayList.SelectMode = confirming ? ItemList.ItemListSelectMode.None : ItemList.ItemListSelectMode.Single;
        var canOpen = !_picking && CanOpenReplay;
        window.PlayButton.Disabled = !canOpen || _selected == null;
        window.BrowseButton.Disabled = !canOpen;
    }

    private void ReportError(LocalReplayLibraryWindow window, LocId message, Exception error)
    {
        _logManager.GetSawmill("replay.library").Error(error.ToString());
        if (_window == window)
            ShowStatus(window, message);
    }

    private static void ShowStatus(LocalReplayLibraryWindow window, LocId message)
    {
        window.StatusLabel.Text = Loc.GetString(message);
        window.StatusLabel.Visible = true;
    }
}
