namespace Content.Server._Aquila.Hydrakin.Components;

/// <summary>
/// Задаёт мощность пассивного нагрева гидракина в обычных и в жарких условиях.
/// </summary>
[RegisterComponent]
public sealed partial class HydrakinHeatRegulationComponent : Component
{
    /// <summary>
    /// Мощность нагрева в обычных условиях, в ваттах.
    /// </summary>
    [DataField]
    public float NormalWatts = 0f;

    /// <summary>
    /// Мощность нагрева, когда надет предмет верхней одежды или среда горячее порога, в ваттах.
    /// </summary>
    [DataField]
    public float HeatedWatts = 3900f;

    /// <summary>
    /// Температура среды, выше которой гидракин считается в горячей среде, в кельвинах.
    /// </summary>
    [DataField]
    public float HotEnvironmentTemperature = 293.15f;
}
