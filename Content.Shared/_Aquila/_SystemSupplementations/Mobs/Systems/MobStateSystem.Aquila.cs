using Content.Shared.Mobs.Components;

namespace Content.Shared.Mobs.Systems;

public partial class MobStateSystem
{
    /// <summary>
    /// Проверяет, находится ли моб в предкритическом состоянии.
    /// </summary>
    /// <param name="target">Целевая сущность</param>
    /// <param name="component">Компонент MobState, принадлежащий цели</param>
    /// <returns>True, если моб в предкритическом состоянии</returns>
    public bool IsPreCritical(EntityUid target, MobStateComponent? component = null)
    {
        if (!_mobStateQuery.Resolve(target, ref component, false))
            return false;
        return component.CurrentState == MobState.PreCritical;
    }
}
