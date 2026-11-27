using Content.Goobstation.Shared._Aquila.Illusion;
using Content.Shared.Explosion;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Illusion;

public sealed partial class IllusionComponent
{
    /// <summary>
    /// Прототип взрыва, которым иллюзия взрывается при атаке и смерти. Null - иллюзия не взрывается.
    /// </summary>
    [Access(typeof(SharedIllusionSystem))]
    public ProtoId<ExplosionPrototype>? ExplosionType;

    /// <summary>
    /// Радиус взрыва иллюзии в тайлах.
    /// </summary>
    [Access(typeof(SharedIllusionSystem))]
    public float ExplosionRadius = 2f;

    /// <summary>
    /// Взорвалась ли иллюзия уже, чтобы она не взрывалась повторно.
    /// </summary>
    [Access(typeof(SharedIllusionSystem))]
    public bool Exploded;
}
