using Content.Server._Aquila.Antags.RoleCleanup.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Antags.RoleCleanup.Components;

/// <summary>
/// Хранит антагонистские роли разума на момент последнего изменения, чтобы понимать, какую именно роль сняли.
/// </summary>
[RegisterComponent]
[Access(typeof(AntagRoleCleanupSystem))]
public sealed partial class AntagRoleTrackingComponent : Component
{
    /// <summary>
    /// Антагонистские роли, которые висели на разуме в прошлый раз.
    /// </summary>
    [ViewVariables]
    public List<ProtoId<AntagPrototype>> KnownRoles = new();
}
