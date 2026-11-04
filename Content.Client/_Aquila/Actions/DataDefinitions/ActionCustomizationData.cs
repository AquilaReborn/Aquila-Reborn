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

    /// <summary>
    /// Порядок действий на панели по ID их прототипов. Хранит и действия, которых сейчас нет у персонажа,
    /// чтобы при их возвращении восстановить место относительно соседей.
    /// </summary>
    [DataField]
    public List<EntProtoId> Priority = new();
}
