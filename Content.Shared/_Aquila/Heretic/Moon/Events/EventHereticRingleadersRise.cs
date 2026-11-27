using Content.Shared.Actions;
using Content.Shared.Explosion;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Heretic.Moon.Events;

/// <summary>
/// Поднимает всех вокруг на безумный праздник: создаёт иллюзорные копии еретика, которые на них нападают.
/// </summary>
public sealed partial class EventHereticRingleadersRise : InstantActionEvent
{
    /// <summary>
    /// Радиус действия.
    /// </summary>
    [DataField]
    public float Radius = 5f;

    /// <summary>
    /// Максимальное число активных иллюзорных копий до возвышения.
    /// </summary>
    [DataField]
    public int IllusionCount = 1;

    /// <summary>
    /// Максимальное число активных иллюзорных копий после возвышения.
    /// </summary>
    [DataField]
    public int AscendedIllusionCount = 3;

    /// <summary>
    /// Сколько живёт каждая иллюзорная копия.
    /// </summary>
    [DataField]
    public TimeSpan IllusionLifetime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Множитель здоровья иллюзорных копий относительно текущего здоровья еретика.
    /// </summary>
    [DataField]
    public float IllusionHealthMultiplier = 1f;

    /// <summary>
    /// Прототип визуального эффекта.
    /// </summary>
    [DataField]
    public EntProtoId Effect = "EffectMoonRingleader";

    /// <summary>
    /// Прототип взрыва, которым иллюзорные копии взрываются при атаке и смерти.
    /// </summary>
    [DataField]
    public ProtoId<ExplosionPrototype> IllusionExplosionType = "Default";

    /// <summary>
    /// Радиус взрыва иллюзорных копий в тайлах. Null - использовать радиус по умолчанию.
    /// </summary>
    [DataField]
    public float? IllusionExplosionRadius;
}
