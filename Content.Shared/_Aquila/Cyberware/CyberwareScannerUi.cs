using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Cyberware;

[Serializable, NetSerializable]
public enum CyberwareScannerUiKey : byte
{
    Key,
}

/// <summary>
/// Результат сканирования имплантов, который показывает окно сканера.
/// </summary>
[Serializable, NetSerializable]
public sealed class CyberwareScannerUiState : BoundUserInterfaceState
{
    /// <summary>
    /// Имя просканированного.
    /// </summary>
    public readonly string TargetName;

    /// <summary>
    /// Было ли сканирование глубоким.
    /// </summary>
    public readonly bool Deep;

    /// <summary>
    /// Сколько имплантов дают нагрузку.
    /// </summary>
    public readonly int ImplantCount;

    /// <summary>
    /// Риск киберпсихоза: нагрузка вместе с напряжением.
    /// </summary>
    public readonly float Risk;

    /// <summary>
    /// Риск, при котором состояние считается критическим. Шкала заполняется до этого значения.
    /// </summary>
    public readonly float CriticalLevel;

    /// <summary>
    /// Название уровня кибернагрузки.
    /// </summary>
    public readonly LocId Level;

    /// <summary>
    /// Привыкает ли организм к недавно установленным имплантам.
    /// </summary>
    public readonly bool Adapting;

    /// <summary>
    /// Названия опознанных подкожных имплантов.
    /// </summary>
    public readonly List<string> Implants;

    /// <summary>
    /// Сколько подкожных имплантов опознать не удалось.
    /// </summary>
    public readonly int Concealed;

    /// <summary>
    /// Название стадии киберпсихоза, если он есть.
    /// </summary>
    public readonly LocId? PsychosisStage;

    /// <summary>
    /// Доля пройденного курса лечения от 0 до 1, если лечение начато.
    /// </summary>
    public readonly float? CureProgress;

    public CyberwareScannerUiState(
        string targetName,
        bool deep,
        int implantCount,
        float risk,
        float criticalLevel,
        LocId level,
        bool adapting,
        List<string> implants,
        int concealed,
        LocId? psychosisStage,
        float? cureProgress)
    {
        TargetName = targetName;
        Deep = deep;
        ImplantCount = implantCount;
        Risk = risk;
        CriticalLevel = criticalLevel;
        Level = level;
        Adapting = adapting;
        Implants = implants;
        Concealed = concealed;
        PsychosisStage = psychosisStage;
        CureProgress = cureProgress;
    }
}
