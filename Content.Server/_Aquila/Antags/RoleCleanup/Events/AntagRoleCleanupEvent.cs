using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Antags.RoleCleanup.Events;

/// <summary>
/// Поднимается на разуме, когда с него сняли антагонистскую роль и она не была сразу заменена другой.
/// </summary>
public sealed class AntagRoleCleanupEvent : EntityEventArgs
{
    /// <summary>
    /// Разум, с которого сняли роль.
    /// </summary>
    public readonly EntityUid MindId;

    /// <summary>
    /// Компонент разума.
    /// </summary>
    public readonly MindComponent Mind;

    /// <summary>
    /// Тело, в котором разум всё ещё находится, или null, если он его покинул.
    /// </summary>
    public readonly EntityUid? Body;

    /// <summary>
    /// Антагонистские роли, которые были сняты.
    /// </summary>
    public readonly IReadOnlyCollection<ProtoId<AntagPrototype>> RemovedRoles;

    /// <summary>
    /// Антагонистские роли, которые остались у разума.
    /// </summary>
    public readonly IReadOnlyCollection<ProtoId<AntagPrototype>> RemainingRoles;

    /// <summary>
    /// Создаёт событие окончательного снятия антагонистской роли.
    /// </summary>
    /// <param name="mindId">Разум, с которого сняли роль.</param>
    /// <param name="mind">Компонент разума.</param>
    /// <param name="body">Тело, в котором разум всё ещё находится.</param>
    /// <param name="removedRoles">Антагонистские роли, которые были сняты.</param>
    /// <param name="remainingRoles">Антагонистские роли, которые остались у разума.</param>
    public AntagRoleCleanupEvent(
        EntityUid mindId,
        MindComponent mind,
        EntityUid? body,
        IReadOnlyCollection<ProtoId<AntagPrototype>> removedRoles,
        IReadOnlyCollection<ProtoId<AntagPrototype>> remainingRoles)
    {
        MindId = mindId;
        Mind = mind;
        Body = body;
        RemovedRoles = removedRoles;
        RemainingRoles = remainingRoles;
    }

    /// <summary>
    /// Проверяет, что с разума сняли указанную антагонистскую роль.
    /// </summary>
    /// <param name="role">Прототип антагониста.</param>
    /// <returns>True, если роль была снята.</returns>
    public bool WasRemoved(ProtoId<AntagPrototype> role)
    {
        foreach (var removed in RemovedRoles)
        {
            if (removed == role)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Проверяет, что с разума сняли хотя бы одну роль из списка.
    /// </summary>
    /// <param name="roles">Прототипы антагонистов.</param>
    /// <returns>True, если хотя бы одна роль была снята.</returns>
    public bool WasAnyRemoved(IReadOnlyCollection<ProtoId<AntagPrototype>> roles)
    {
        foreach (var role in roles)
        {
            if (WasRemoved(role))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Проверяет, осталась ли у разума указанная антагонистская роль.
    /// </summary>
    /// <param name="role">Прототип антагониста.</param>
    /// <returns>True, если роль всё ещё висит на разуме.</returns>
    public bool HasRemainingRole(ProtoId<AntagPrototype> role)
    {
        foreach (var remaining in RemainingRoles)
        {
            if (remaining == role)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Проверяет, осталась ли у разума хотя бы одна роль из списка.
    /// </summary>
    /// <param name="roles">Прототипы антагонистов.</param>
    /// <returns>True, если хотя бы одна роль ещё висит на разуме.</returns>
    public bool HasAnyRemainingRole(IReadOnlyCollection<ProtoId<AntagPrototype>> roles)
    {
        foreach (var role in roles)
        {
            if (HasRemainingRole(role))
                return true;
        }

        return false;
    }
}
