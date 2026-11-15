using Content.Client._Aquila.Actions;
using Content.Shared.Actions.Components;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed partial class ActionUIController
{
    private ActionCustomizationUIController ActionCustomization => UIManager.GetUIController<ActionCustomizationUIController>();

    /// <summary>
    /// Активирует действие так же, как слот панели: у целевых действий переключается режим выбора цели.
    /// </summary>
    public void TriggerAction(Entity<ActionComponent> action)
    {
        if (EntityManager.TryGetComponent<TargetActionComponent>(action, out var target))
            ToggleTargeting((action, action, target));
        else
            _actionsSystem?.TriggerAction(action);
    }

    private void ApplyActionPriority()
    {
        ActionCustomization.ApplyActionPriority(_actions);
    }

    private void InsertActionByPriority(EntityUid action)
    {
        _actions.Insert(ActionCustomization.GetPriorityInsertIndex(_actions, action), action);
    }

    private void SaveActionPriority()
    {
        ActionCustomization.UpdateActionPriority(_actions);
    }
}
