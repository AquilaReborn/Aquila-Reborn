using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Antags.RoleCleanup.Components;

/// <summary>
/// Отмечает разум, у которого сняли антагонистскую роль, чтобы проверить снятие в конце текущего тика.
/// Нужен, чтобы правила не принимали замену одной роли на другую за окончательное снятие.
/// </summary>
[RegisterComponent]
[Access(typeof(AntagRoleCleanupSystem))]
public sealed partial class AntagRoleCleanupPendingComponent : Component
{
    /// <summary>
    /// Антагонистские роли, снятые с разума с прошлой проверки.
    /// </summary>
    [ViewVariables]
    public List<ProtoId<AntagPrototype>> RemovedRoles = new();
}
