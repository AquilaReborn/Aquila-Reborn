namespace Content.Server._Aquila.Cyberware.Components;

/// <summary>
/// Временная защита от киберпсихоза: шанс перейти на стадию тяжелее ниже обычного.
/// </summary>
[RegisterComponent, Access(typeof(CyberpsychosisResistanceSystem)), AutoGenerateComponentPause]
public sealed partial class CyberpsychosisResistanceComponent : Component
{
    /// <summary>
    /// Когда защита закончится.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Во сколько раз сейчас умножается шанс перехода на следующую стадию.
    /// </summary>
    [ViewVariables]
    public float Multiplier = 1f;
}
