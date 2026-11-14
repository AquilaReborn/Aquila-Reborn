using Content.Client.Eui;
using Content.Shared._Aquila.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Aquila.Administration;

[UsedImplicitly]
public sealed class AdminCharacterSpawnEui : BaseEui
{
    private readonly AdminCharacterSpawnWindow _window = new();

    /// <summary>Подключает окно к серверному интерфейсу.</summary>
    public AdminCharacterSpawnEui()
    {
        _window.OnConfirm += SendMessage;
        _window.OnClose += OnWindowClosed;
    }

    /// <inheritdoc/>
    public override void Opened()
    {
        _window.OpenCentered();
    }

    /// <inheritdoc/>
    public override void Closed()
    {
        _window.OnClose -= OnWindowClosed;
        _window.OnConfirm -= SendMessage;
        _window.Close();
        _window.Release();
    }

    /// <inheritdoc/>
    public override void HandleState(EuiStateBase state)
    {
        if (state is AdminCharacterSpawnState spawnState)
            _window.UpdateState(spawnState);
    }

    /// <inheritdoc/>
    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is AdminCharacterSpawnFailure)
            _window.ShowError();
    }

    private void OnWindowClosed()
    {
        _window.ClearPreview();
        SendMessage(new CloseEuiMessage());
    }
}
