using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.HolyCorpse.DataDefinitions;

/// <summary>
/// Набор, который носитель получает при первом вживлении конечности: страж и компоненты.
/// </summary>
[DataDefinition]
public sealed partial class HolyCorpseSet
{
    /// <summary>
    /// Прототип стража, который появляется у носителя.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId GuardianProto;

    /// <summary>
    /// Компоненты, которые добавляются носителю и снимаются при извлечении конечностей.
    /// </summary>
    [DataField]
    public ComponentRegistry? Components;
}
