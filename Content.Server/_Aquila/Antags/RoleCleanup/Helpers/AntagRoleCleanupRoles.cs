using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Antags.RoleCleanup.Helpers;

/// <summary>
/// Наборы антагонистских ролей, которые правила делят между собой при снятии ролей.
/// </summary>
public static class AntagRoleCleanupRoles
{
    /// <summary>
    /// Роль ядерного оперативника.
    /// </summary>
    public static readonly ProtoId<AntagPrototype> Nukeops = "Nukeops";

    /// <summary>
    /// Роль медика ядерных оперативников.
    /// </summary>
    public static readonly ProtoId<AntagPrototype> NukeopsMedic = "NukeopsMedic";

    /// <summary>
    /// Роль командира ядерных оперативников.
    /// </summary>
    public static readonly ProtoId<AntagPrototype> NukeopsCommander = "NukeopsCommander";

    /// <summary>
    /// Все роли, при которых у тела остаётся аплинк ядерных оперативников.
    /// </summary>
    public static readonly ProtoId<AntagPrototype>[] AllNukeops =
    [
        Nukeops,
        NukeopsMedic,
        NukeopsCommander,
    ];
}
