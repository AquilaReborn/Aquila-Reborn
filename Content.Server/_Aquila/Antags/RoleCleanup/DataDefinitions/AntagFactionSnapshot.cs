using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Antags.RoleCleanup.DataDefinitions;

/// <summary>
/// Снимок фракций тела на момент выдачи одной антагонистской роли.
/// </summary>
public sealed class AntagFactionSnapshot
{
    /// <summary>
    /// Прототип антагониста, которому принадлежит снимок.
    /// </summary>
    public ProtoId<AntagPrototype> Role;

    /// <summary>
    /// Была ли у тела фракция вообще до выдачи роли.
    /// </summary>
    public bool HadComponent;

    /// <summary>
    /// Фракции, в которых тело состояло до выдачи роли.
    /// </summary>
    public List<ProtoId<NpcFactionPrototype>> Factions = new();
}
