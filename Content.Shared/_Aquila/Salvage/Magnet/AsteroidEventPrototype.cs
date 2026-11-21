using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Salvage.Magnet;

/// <summary>
/// Модификатор, который иногда накладывается на притянутый магнитом астероид.
/// </summary>
[Prototype]
public sealed partial class AsteroidEventPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Относительный вес события в общем пуле. Событие без слоёв и без длительности оставляет астероид обычным.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Дополнительные слои генерации, которые добавляются к конфигу астероида.
    /// Таблица мобов задаётся содержимым слоя <see cref="Content.Shared.Procedural.DungeonLayers.MobsDunGen"/>.
    /// </summary>
    [DataField]
    public List<IDunGenLayer> Layers = new();

    /// <summary>
    /// Время удержания астероида у магнита вместо обычного, если задано.
    /// </summary>
    [DataField]
    public TimeSpan? Duration;
}
