using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Engram;

/// <summary>
/// Душегуб: вытягивает личность из мозга мёртвого или умирающего человека и записывает её на чип энграммы.
/// </summary>
[RegisterComponent]
public sealed partial class SoulkillerComponent : Component
{
    /// <summary>
    /// Сколько длится извлечение личности.
    /// </summary>
    [DataField]
    public TimeSpan ExtractTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Чип, на который записывается энграмма.
    /// </summary>
    [DataField]
    public EntProtoId ChipPrototype = "EngramChip";

    /// <summary>
    /// Шаблон с компонентами энграммы, которые добавляются к манекену нужного вида.
    /// </summary>
    [DataField]
    public EntProtoId EngramTemplate = "EngramTemplate";

    /// <summary>
    /// Вид, чей манекен берётся, если у жертвы нет внешности, например у отдельного мозга.
    /// </summary>
    [DataField]
    public ProtoId<SpeciesPrototype> DefaultSpecies = "Human";

    /// <summary>
    /// Слоты, одежда из которых копируется на энграмму.
    /// </summary>
    [DataField]
    public List<string> CopiedSlots = new()
    {
        "jumpsuit",
        "outerClothing",
        "head",
        "shoes",
        "gloves",
        "mask",
        "neck",
        "eyes",
    };
}
