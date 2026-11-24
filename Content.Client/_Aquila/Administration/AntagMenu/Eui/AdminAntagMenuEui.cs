using Content.Client._Aquila.Administration.AntagMenu.UI;
using Content.Client.Eui;
using Content.Shared._Aquila.Administration.AntagMenu.Eui;
using Content.Shared._Aquila.Administration.AntagMenu.Messages;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Aquila.Administration.AntagMenu.Eui;

/// <summary>
/// Клиентский интерфейс меню антагонистов, связывающий окно с сервером.
/// </summary>
[UsedImplicitly]
public sealed class AdminAntagMenuEui : BaseEui
{
    private readonly AdminAntagMenuWindow _window = new();

    /// <summary>
    /// Создаёт окно и подписывает его события на отправку сообщений серверу.
    /// </summary>
    public AdminAntagMenuEui()
    {
        _window.OnClose += OnWindowClosed;
        _window.OnRefresh += OnRefresh;
        _window.OnAddObjective += OnAddObjective;
        _window.OnRequestObjectives += OnRequestObjectives;
        _window.OnRemoveObjective += OnRemoveObjective;
        _window.OnSetObjectiveTarget += OnSetObjectiveTarget;
        _window.OnRemoveAllObjectives += OnRemoveAllObjectives;
        _window.OnGrantRole += OnGrantRole;
        _window.OnRemoveRole += OnRemoveRole;
    }

    /// <inheritdoc/>
    public override void Opened()
    {
        base.Opened();

        _window.OpenCentered();
        SendMessage(new AdminAntagMenuRefreshMessage());
    }

    /// <inheritdoc/>
    public override void Closed()
    {
        base.Closed();

        _window.OnClose -= OnWindowClosed;
        _window.OnRefresh -= OnRefresh;
        _window.OnAddObjective -= OnAddObjective;
        _window.OnRequestObjectives -= OnRequestObjectives;
        _window.OnRemoveObjective -= OnRemoveObjective;
        _window.OnSetObjectiveTarget -= OnSetObjectiveTarget;
        _window.OnRemoveAllObjectives -= OnRemoveAllObjectives;
        _window.OnGrantRole -= OnGrantRole;
        _window.OnRemoveRole -= OnRemoveRole;
        _window.Close();
    }

    /// <inheritdoc/>
    public override void HandleState(EuiStateBase state)
    {
        if (state is AdminAntagMenuEuiState menuState)
            _window.UpdateState(menuState);
    }

    /// <inheritdoc/>
    public override void HandleMessage(EuiMessageBase msg)
    {
        switch (msg)
        {
            case AdminAntagMenuErrorMessage error:
                _window.ShowError(error.MessageKey);
                break;
            case AdminAntagMenuObjectivesMessage objectives:
                _window.UpdateObjectiveOptions(objectives.Mind, objectives.Prototypes);
                break;
        }
    }

    private void OnRefresh()
    {
        SendMessage(new AdminAntagMenuRefreshMessage());
    }

    private void OnAddObjective(NetEntity mind, string prototype)
    {
        SendMessage(new AdminAntagMenuAddObjectiveMessage(mind, prototype));
    }

    private void OnRequestObjectives(NetEntity mind)
    {
        SendMessage(new AdminAntagMenuRequestObjectivesMessage(mind));
    }

    private void OnRemoveObjective(NetEntity mind, NetEntity objective)
    {
        SendMessage(new AdminAntagMenuRemoveObjectiveMessage(mind, objective));
    }

    private void OnSetObjectiveTarget(NetEntity mind, NetEntity objective, int value)
    {
        SendMessage(new AdminAntagMenuSetObjectiveTargetMessage(mind, objective, value));
    }

    private void OnRemoveAllObjectives(NetEntity mind)
    {
        SendMessage(new AdminAntagMenuRemoveAllObjectivesMessage(mind));
    }

    private void OnGrantRole(NetEntity mind, string antag)
    {
        SendMessage(new AdminAntagMenuGrantRoleMessage(mind, antag));
    }

    private void OnRemoveRole(NetEntity mind, string antag)
    {
        SendMessage(new AdminAntagMenuRemoveRoleMessage(mind, antag));
    }

    private void OnWindowClosed()
    {
        SendMessage(new CloseEuiMessage());
    }
}
