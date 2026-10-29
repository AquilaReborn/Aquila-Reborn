using Content.Server.NPC;

namespace Content.Server._Aquila.NPC;

public static class HtnTargetHelper
{
    /// <summary>
    /// Достаёт цель из блэкборда и проверяет, что сущность ещё существует.
    /// Нужно, когда цель успела пропасть из блэкборда или удалиться между планированием и запуском оператора.
    /// </summary>
    public static bool TryGetExistingTarget(NPCBlackboard blackboard, string key, IEntityManager entManager, out EntityUid target)
    {
        return blackboard.TryGetValue<EntityUid>(key, out target, entManager) && entManager.EntityExists(target);
    }
}
