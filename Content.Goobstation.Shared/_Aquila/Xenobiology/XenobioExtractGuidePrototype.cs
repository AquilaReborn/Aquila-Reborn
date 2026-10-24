using Content.Goobstation.Shared._Aquila.Xenobiology.DataDefinitions;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology;

/// <summary>
/// Описание реакций экстракта слайма для руководства.
/// </summary>
[Prototype]
public sealed partial class XenobioExtractGuidePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Экстракт, реакции которого описываются.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Extract;

    /// <summary>
    /// Уровень экстракта, по нему руководство группирует экстракты.
    /// </summary>
    [DataField]
    public int Tier;

    /// <summary>
    /// Реакции экстракта в порядке показа.
    /// </summary>
    [DataField(required: true)]
    public List<XenobioExtractGuideReaction> Reactions = new();
}
