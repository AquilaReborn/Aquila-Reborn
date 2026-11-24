using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Aquila.Kitchen;

/// <summary>
/// Мясорубка: принимает труп, перемалывает его и выдаёт только мясо, без органов.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class MeatGrinderComponent : Component
{
    /// <summary>
    /// Контейнер, в котором лежит труп во время перемалывания.
    /// </summary>
    [DataField]
    public string ContainerId = "grinder_body";

    /// <summary>
    /// Время, за которое существо затаскивается в машину.
    /// </summary>
    [DataField]
    public TimeSpan LoadTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Базовое время перемалывания трупа.
    /// </summary>
    [DataField]
    public TimeSpan GrindTime = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Дополнительное время перемалывания на единицу массы трупа.
    /// </summary>
    [DataField]
    public TimeSpan TimePerMass = TimeSpan.FromSeconds(0.02);

    /// <summary>
    /// Амплитуда тряски машины, пока она перемалывает труп.
    /// </summary>
    [DataField]
    public float JitterAmplitude = -10f;

    /// <summary>
    /// Частота тряски машины, пока она перемалывает труп.
    /// </summary>
    [DataField]
    public float JitterFrequency = 50f;

    /// <summary>
    /// Во сколько раз больше мяса даёт мясорубка по сравнению с обычной разделкой.
    /// Итоговое количество округляется вверх.
    /// </summary>
    [DataField]
    public float MeatMultiplier = 1.5f;

    /// <summary>
    /// Мясо, которое выдаётся, если у существа в разделке нет ни одного вида мяса.
    /// </summary>
    [DataField]
    public EntProtoId FallbackMeat = "FoodMeat";

    /// <summary>
    /// Сколько <see cref="FallbackMeat"/> выдаётся существу без мяса в разделке.
    /// </summary>
    [DataField]
    public int FallbackMeatAmount = 2;

    /// <summary>
    /// Звук включения машины.
    /// </summary>
    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    /// Звук работы, играет всю обработку трупа.
    /// </summary>
    [DataField]
    public SoundSpecifier GrindSound = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

    /// <summary>
    /// Звук, который играет, когда мясо уже выпало.
    /// </summary>
    [DataField]
    public SoundSpecifier FinishSound = new SoundCollectionSpecifier("gib");

    /// <summary>
    /// Машина уже перемалывала труп, поэтому её спрайт запачкан кровью.
    /// </summary>
    [DataField, ViewVariables]
    public bool Bloody;

    /// <summary>
    /// До какого момента идёт перемалывание. null - машина свободна.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? GrindEnd;

    /// <summary>
    /// Существо, которое сейчас затаскивают в машину. Пока поле не null, машина занята и второе существо
    /// не примет. null - никто не затаскивается.
    /// </summary>
    [ViewVariables]
    public EntityUid? Loading;

    /// <summary>
    /// Поток звука работы.
    /// </summary>
    [ViewVariables]
    public EntityUid? GrindStream;
}
