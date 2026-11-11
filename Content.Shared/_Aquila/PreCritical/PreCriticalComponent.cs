using Content.Goobstation.Maths.FixedPoint;

namespace Content.Shared._Aquila.PreCritical;

/// <summary>
/// Настройки предкрита живого персонажа.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class PreCriticalComponent : Component
{
    /// <summary>
    /// Порог урона жизненно важных частей тела.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = 100;

    /// <summary>
    /// Множитель скорости поверх обычной скорости ползания.
    /// </summary>
    [DataField]
    public float CrawlSpeedMultiplier = 0.5f;
}
