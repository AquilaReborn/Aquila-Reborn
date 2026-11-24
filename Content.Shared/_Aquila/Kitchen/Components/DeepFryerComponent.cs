using Content.Shared._Aquila.Kitchen.Prototypes;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Aquila.Kitchen;

/// <summary>
/// Фритюрница: держит еду в корзине и жарит всю партию разом.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class DeepFryerComponent : Component
{
    /// <summary>
    /// Идентификатор контейнера, в котором лежит еда.
    /// </summary>
    [DataField]
    public string ContainerId = "fryer_storage";

    /// <summary>
    /// Сколько предметов помещается в корзину.
    /// </summary>
    [DataField]
    public int Capacity = 8;

    /// <summary>
    /// Максимальный размер предмета, который влезает в корзину.
    /// </summary>
    [DataField]
    public ProtoId<ItemSizePrototype> MaxItemSize = "Normal";

    /// <summary>
    /// Сколько длится одна партия жарки.
    /// </summary>
    [DataField]
    public TimeSpan FryTime = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Рецепты жарки: что во что превращается.
    /// </summary>
    [DataField]
    public List<ProtoId<DeepFryRecipePrototype>> Recipes = new();

    /// <summary>
    /// Звук включения.
    /// </summary>
    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    /// Звук, который играет всю жарку.
    /// </summary>
    [DataField]
    public SoundSpecifier FryingSound = new SoundPathSpecifier("/Audio/Effects/sizzle.ogg");

    /// <summary>
    /// Звук готовности партии.
    /// </summary>
    [DataField]
    public SoundSpecifier DoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

    /// <summary>
    /// До какого момента жарится текущая партия. null - машина свободна.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? FryEnd;

    /// <summary>
    /// Партия готова и лежит в корзине: следующее взаимодействие выложит её, а не пожарит заново.
    /// Закладка новой еды сбрасывает флаг, чтобы следующая активация жарила корзину.
    /// </summary>
    [ViewVariables]
    public bool BatchDone;

    /// <summary>
    /// Поток звука жарки.
    /// </summary>
    [ViewVariables]
    public EntityUid? FryingStream;
}
