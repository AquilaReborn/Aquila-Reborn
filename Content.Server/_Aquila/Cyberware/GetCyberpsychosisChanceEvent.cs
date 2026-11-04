namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Поднимается на теле перед броском на переход киберпсихоза в стадию тяжелее, чтобы собрать модификаторы шанса.
/// </summary>
[ByRefEvent]
public record struct GetCyberpsychosisChanceEvent(float Multiplier)
{
    /// <summary>
    /// Во сколько раз умножить шанс перехода.
    /// </summary>
    public float Multiplier = Multiplier;
}
