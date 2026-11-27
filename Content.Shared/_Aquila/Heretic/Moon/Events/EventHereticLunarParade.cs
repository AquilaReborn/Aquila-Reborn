using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Heretic.Moon.Events;

/// <summary>
/// Запускает Лунный парад: снаряд, который пробивает всех на пути и накладывает безумие.
/// </summary>
public sealed partial class EventHereticLunarParade : WorldTargetActionEvent
{
    /// <summary>
    /// Прототип снаряда.
    /// </summary>
    [DataField]
    public EntProtoId Projectile = "ProjectileMoonParade";

    /// <summary>
    /// Скорость снаряда.
    /// </summary>
    [DataField]
    public float ProjectileSpeed = 2f;
}
