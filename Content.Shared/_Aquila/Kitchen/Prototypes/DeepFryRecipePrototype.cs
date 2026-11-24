using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Kitchen.Prototypes;

/// <summary>
/// Рецепт фритюрницы: какой прототип во что превращается после жарки.
/// </summary>
[Prototype]
public sealed partial class DeepFryRecipePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Прототип предмета, который кладут во фритюрницу.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Source;

    /// <summary>
    /// Прототип, который получается после жарки.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Result;
}
