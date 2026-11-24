using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.DataDefinitions;

/// <summary>
/// Разум игрока с его антагонистскими ролями и целями.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuMindData
{
    /// <summary>
    /// Сущность разума.
    /// </summary>
    public NetEntity Mind;

    /// <summary>
    /// Имя персонажа. Может быть пустым, если у разума нет тела.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Имя игрока, если разум принадлежит игроку.
    /// </summary>
    public string Username = string.Empty;

    /// <summary>
    /// Прототипы антагонистских ролей разума.
    /// </summary>
    public List<string> Roles = new();

    /// <summary>
    /// Цели разума.
    /// </summary>
    public List<AdminAntagMenuObjectiveData> Objectives = new();
}
