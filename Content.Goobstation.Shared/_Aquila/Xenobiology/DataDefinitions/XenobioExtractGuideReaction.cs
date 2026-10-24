using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared._Aquila.Xenobiology.DataDefinitions;

[DataDefinition]
public sealed partial class XenobioExtractGuideReaction
{
    /// <summary>
    /// Реагент, который запускает реакцию.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>
    /// Описание эффекта реакции.
    /// </summary>
    [DataField(required: true)]
    public LocId Description;
}
