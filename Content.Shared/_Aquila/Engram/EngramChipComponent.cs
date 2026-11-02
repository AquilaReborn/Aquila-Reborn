namespace Content.Shared._Aquila.Engram;

/// <summary>
/// Чип с записанной энграммой. Пока чип не вставлен в голову, личность заперта внутри.
/// </summary>
[RegisterComponent]
public sealed partial class EngramChipComponent : Component
{
    /// <summary>
    /// Контейнер чипа, в котором хранится энграмма.
    /// </summary>
    [DataField]
    public string ContainerId = "engram";

    /// <summary>
    /// Сколько длится установка чипа себе.
    /// </summary>
    [DataField]
    public TimeSpan InsertTime = TimeSpan.FromSeconds(3);
}
