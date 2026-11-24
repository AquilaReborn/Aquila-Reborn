using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.DataDefinitions;

/// <summary>
/// Цель разума для отображения в меню антагонистов.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuObjectiveData
{
    /// <summary>
    /// Сущность цели.
    /// </summary>
    public NetEntity Objective;

    /// <summary>
    /// Прототип цели.
    /// </summary>
    public string Prototype = string.Empty;

    /// <summary>
    /// Название цели, уже собранное сервером.
    /// </summary>
    public string Title = string.Empty;

    /// <summary>
    /// Описание цели, уже собранное сервером.
    /// </summary>
    public string Description = string.Empty;

    /// <summary>
    /// Прогресс цели от 0 до 1.
    /// </summary>
    public float Progress;

    /// <summary>
    /// Требуемое значение для числовой цели, если она есть.
    /// </summary>
    public int? Target;

    /// <summary>
    /// Текущее значение для числовой цели, если она есть.
    /// </summary>
    public int? Current;
}
