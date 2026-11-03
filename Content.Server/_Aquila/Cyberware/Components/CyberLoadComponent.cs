using Content.Shared._Aquila.Cyberware.Prototypes;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Cyberware.Components;

/// <summary>
/// Кибернагрузка тела: сколько в нём стоит железа и насколько организм ещё не привык к последним установкам.
/// </summary>
[RegisterComponent, Access(typeof(CyberLoadSystem), typeof(CyberpsychosisSystem), typeof(CyberwareEmpSystem)), AutoGenerateComponentPause]
public sealed partial class CyberLoadComponent : Component
{
    /// <summary>
    /// Постоянная нагрузка, сумма нагрузок всех установленных имплантов.
    /// </summary>
    [ViewVariables]
    public float Load;

    /// <summary>
    /// Напряжение от свежих установок, со временем спадает.
    /// </summary>
    [ViewVariables]
    public float Strain;

    /// <summary>
    /// Сколько имплантов дают нагрузку.
    /// </summary>
    [ViewVariables]
    public int ImplantCount;

    /// <summary>
    /// Нужно ли пересчитать нагрузку в ближайшем обновлении.
    /// </summary>
    [ViewVariables]
    public bool NeedsRecalculation;

    /// <summary>
    /// Не добавлять напряжение при ближайшем пересчёте, даже если нагрузка выросла.
    /// </summary>
    [ViewVariables]
    public bool IgnoreNextIncrease;

    /// <summary>
    /// Во сколько раз напряжение от установки больше нагрузки самого импланта.
    /// </summary>
    [DataField]
    public float StrainMultiplier = 0.8f;

    /// <summary>
    /// Сколько напряжения спадает само по себе за секунду.
    /// </summary>
    [DataField]
    public float StrainDecay = 0.1f;

    /// <summary>
    /// С какого риска организм начинает подавать первые тревожные сигналы.
    /// </summary>
    [DataField]
    public float WarningLevel = 60f;

    /// <summary>
    /// Риск, выше которого возможен киберпсихоз.
    /// </summary>
    [DataField]
    public float Threshold = 100f;

    /// <summary>
    /// Риск, при котором состояние считается критическим.
    /// </summary>
    [DataField]
    public float CriticalLevel = 150f;

    /// <summary>
    /// Когда в следующий раз снижать напряжение.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Стадии киберпсихоза по порядку, от лёгкой к тяжёлой.
    /// </summary>
    [DataField]
    public List<ProtoId<CyberpsychosisStagePrototype>> Stages = new()
    {
        "CyberpsychosisInstability",
        "CyberpsychosisRejection",
        "CyberpsychosisBreakdown",
    };

    /// <summary>
    /// Как часто проверять, не сменилась ли стадия.
    /// </summary>
    [DataField]
    public TimeSpan StageCheckInterval = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Сколько после пробития экранирования импланты уязвимы к следующему ЭМИ.
    /// </summary>
    [DataField]
    public TimeSpan EmpVulnerableWindow = TimeSpan.FromSeconds(15);

    /// <summary>
    /// На сколько ЭМИ отключает импланты, если пробил экранирование.
    /// </summary>
    [DataField]
    public TimeSpan EmpDisableTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Тип урона, который ЭМИ наносит телу с отключёнными имплантами.
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype> EmpDamageType = "Shock";

    /// <summary>
    /// Урон от ЭМИ, который получает тело при отключении имплантов, без учёта нагрузки.
    /// </summary>
    [DataField]
    public float EmpDamageBase = 5f;

    /// <summary>
    /// Сколько урона добавляет каждая единица кибернагрузки.
    /// </summary>
    [DataField]
    public float EmpDamagePerLoad = 0.1f;

    /// <summary>
    /// Сколько напряжения добавляет каждая единица кибернагрузки, когда ЭМИ отключает импланты.
    /// </summary>
    [DataField]
    public float EmpStrainPerLoad = 0.1f;

    /// <summary>
    /// До какого момента импланты уязвимы к ЭМИ после пробития экранирования.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan EmpVulnerableUntil;

    /// <summary>
    /// Тик, в который пробили экранирование. Остальные импланты, задетые тем же импульсом, ещё защищены.
    /// </summary>
    [ViewVariables]
    public GameTick EmpShieldBrokenTick;

    /// <summary>
    /// Тик, в который тело последний раз получило урон от ЭМИ, чтобы один импульс не бил несколько раз.
    /// </summary>
    [ViewVariables]
    public GameTick LastEmpDamageTick;

    /// <summary>
    /// Сколько после излечения киберпсихоз не может начаться снова.
    /// </summary>
    [DataField]
    public TimeSpan CureGrace = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Когда в следующий раз проверять стадию.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextStageCheck;
}
