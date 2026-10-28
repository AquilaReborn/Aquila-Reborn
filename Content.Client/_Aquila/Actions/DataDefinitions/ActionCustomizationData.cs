using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Actions.DataDefinitions;

[DataDefinition]
public sealed partial class ActionCustomizationData
{
    /// <summary>
    /// Настройки действий по ID их прототипов.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, ActionCustomizationEntry> Actions = new();
}
