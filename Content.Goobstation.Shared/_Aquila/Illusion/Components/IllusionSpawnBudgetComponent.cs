namespace Content.Goobstation.Shared._Aquila.Illusion.Components;

/// <summary>
/// Хранит общий список ограниченных иллюзий одного владельца.
/// </summary>
[RegisterComponent, Access(typeof(SharedIllusionSystem))]
public sealed partial class IllusionSpawnBudgetComponent : Component
{
    /// <summary>
    /// Копии, занимающие места в общем лимите.
    /// </summary>
    public List<EntityUid> Illusions = new();

    /// <summary>
    /// Места, зарезервированные на время незавершённого клонирования.
    /// </summary>
    public int Reservations;
}
