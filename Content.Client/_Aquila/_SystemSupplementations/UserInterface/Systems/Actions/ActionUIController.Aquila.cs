using Content.Shared.Actions.Components;

namespace Content.Client.UserInterface.Systems.Actions;

public sealed partial class ActionUIController
{
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
}
