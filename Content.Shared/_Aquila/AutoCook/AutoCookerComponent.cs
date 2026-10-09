using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Aquila.AutoCook;

/// <summary>
/// Машина, которая сама, этап за этапом, готовит выбранный рецепт.
/// Бар и химия синтезируют реагенты в стакан, кухня готовит блюда из хранилища.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class AutoCookerComponent : Component
{
    public const string BeakerSlotId = "autoCookBeakerSlot";

    [DataField(required: true)]
    public AutoCookKind Kind;

    /// <summary>
    /// Реагенты, которые машина синтезирует из энергии, а не через реакции.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> BaseReagents = new();

    /// <summary>
    /// Реагенты без реакций получения с этими группами метаболизма тоже считаются базовыми.
    /// </summary>
    [DataField]
    public List<ProtoId<MetabolismGroupPrototype>> BaseMetabolisms = new();

    /// <summary>
    /// Группы реагентов (<see cref="ReagentPrototype.Group"/>), которые машина умеет готовить.
    /// </summary>
    [DataField]
    public List<string> TargetGroups = new();

    [DataField]
    public List<ProtoId<MetabolismGroupPrototype>> TargetMetabolisms = new();

    [DataField]
    public List<ProtoId<ReagentPrototype>> ExcludedReagents = new();

    /// <summary>
    /// Максимум реакций в цепочке синтеза. Более сложные реагенты недоступны, если их нет в <see cref="ComplexWhitelist"/>.
    /// </summary>
    [DataField]
    public int? MaxReactions;

    [DataField]
    public List<ProtoId<ReagentPrototype>> ComplexWhitelist = new();

    /// <summary>
    /// Показывать <see cref="BaseReagents"/> как отдельные рецепты, чтобы их можно было налить напрямую.
    /// </summary>
    [DataField]
    public bool ListBaseReagents;

    /// <summary>
    /// Раствор-буфер с реагентами от игрока или автоматизации. Готовые промежуточные реагенты
    /// берутся из него вместо синтеза и не считаются в <see cref="MaxReactions"/>.
    /// </summary>
    [DataField]
    public string? BufferSolution;

    [DataField]
    public List<ProtoId<MixingCategoryPrototype>> AllowedMixing = new();

    /// <summary>
    /// Ингредиенты рецептов, которые не расходуются и не требуются, например посуда.
    /// </summary>
    [DataField]
    public List<EntProtoId> FreeIngredients = new();

    [DataField]
    public int MaxQueue = 5;

    [DataField]
    public float WorkingLoad = 400f;

    [DataField]
    public TimeSpan DoneDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(0.2);

    [DataField]
    public int MaxOrderAmount = 1000;

    [DataField]
    public TimeSpan SynthesisDuration = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan SynthesisDurationPerUnit = TimeSpan.FromSeconds(0.04);

    [DataField]
    public TimeSpan BufferTakeDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan ReactionDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan ConditionDuration = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan PrepareDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan SliceDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Доля времени готовки из рецепта микроволновки, которую тратит машина.
    /// </summary>
    [DataField]
    public float CookTimeMultiplier = 0.5f;

    [DataField]
    public TimeSpan MaxCookDuration = TimeSpan.FromSeconds(15);

    [DataField]
    public string StorageContainer = "storagebase";

    [DataField]
    public string PantrySolution = "pantry";

    [DataField]
    public string ProcessingContainer = "autoCookProcessing";

    [DataField]
    public ItemSlot BeakerSlot = new();

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public SoundSpecifier StepSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField]
    public SoundSpecifier DoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

    [ViewVariables]
    public AutoCookJob? Job;

    [ViewVariables]
    public readonly List<AutoCookOrder> Queue = new();

    /// <summary>
    /// Последний заказ, его повторяет сигнал запуска от автоматизации.
    /// </summary>
    [ViewVariables]
    public AutoCookOrder? LastOrder;

    /// <summary>
    /// До этого времени машина показывает, что заказ готов.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? DoneUntil;

    [ViewVariables]
    public bool UiDirty;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUiUpdate;

    [ViewVariables]
    public List<AutoCookRecipeEntry>? SentRecipes;

    [ViewVariables]
    public readonly HashSet<EntityUid> RecipeViewers = new();
}
