using Content.Server._Aquila.Antags.RoleCleanup.DataDefinitions;
using Content.Server._Aquila.Antags.RoleCleanup.Systems;

namespace Content.Server._Aquila.Antags.RoleCleanup.Components;

/// <summary>
/// Хранит наборы фракций, которые были у тела до выдачи антагонистских ролей, чтобы вернуть их при снятии роли.
/// </summary>
[RegisterComponent]
[Access(typeof(AntagRoleCleanupSystem))]
public sealed partial class AntagFactionSnapshotComponent : Component
{
    /// <summary>
    /// Снимки фракций в порядке выдачи ролей.
    /// </summary>
    [ViewVariables]
    public List<AntagFactionSnapshot> Snapshots = new();
}
