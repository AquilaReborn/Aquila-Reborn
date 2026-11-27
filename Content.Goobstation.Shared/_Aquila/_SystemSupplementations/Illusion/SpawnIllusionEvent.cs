using Content.Shared.Explosion;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Illusion;

/// <summary>
/// Запрос на создание иллюзорных копий сущности.
/// </summary>
[ByRefEvent]
public struct SpawnIllusionEvent
{
    /// <summary>
    /// Сущность, с которой копируются иллюзии.
    /// </summary>
    public EntityUid Source;

    /// <summary>
    /// Сколько копий нужно создать.
    /// </summary>
    public int Count;

    /// <summary>
    /// Сколько живёт каждая копия.
    /// </summary>
    public TimeSpan Lifetime;

    /// <summary>
    /// Множитель здоровья копий относительно текущего здоровья источника.
    /// </summary>
    public float HealthMultiplier;

    /// <summary>
    /// Цель, на которую копии будут нападать. Если не задана, копии останутся без цели.
    /// </summary>
    public EntityUid? Target;

    /// <summary>
    /// Цели для последовательного распределения копий. Если список пуст, используется отдельная цель.
    /// </summary>
    public IReadOnlyList<EntityUid>? Targets;

    /// <summary>
    /// Владелец общего лимита копий. Null отключает ограничение.
    /// </summary>
    public EntityUid? BudgetOwner;

    /// <summary>
    /// Максимальное число одновременно существующих копий владельца.
    /// </summary>
    public int? ActiveLimit;

    /// <summary>
    /// Прототип взрыва, которым копия взрывается при атаке и смерти. Null - копия не взрывается.
    /// </summary>
    public ProtoId<ExplosionPrototype>? ExplosionType;

    /// <summary>
    /// Радиус взрыва копии в тайлах. Null - используется радиус из <see cref="IllusionComponent"/>.
    /// </summary>
    public float? ExplosionRadius;
}
