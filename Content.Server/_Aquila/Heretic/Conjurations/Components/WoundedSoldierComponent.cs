using Content.Goobstation.Maths.FixedPoint;
using Content.Server._Aquila.Heretic.Conjurations.Systems;

namespace Content.Server._Aquila.Heretic.Conjurations.Components;

/// <summary>
/// Носитель эффекта раненого солдата: постепенно залечивает механические и термические повреждения.
/// </summary>
[RegisterComponent, Access(typeof(WoundedSoldierSystem)), AutoGenerateComponentPause]
public sealed partial class WoundedSoldierComponent : Component
{
    /// <summary>
    /// Промежуток между порциями лечения.
    /// </summary>
    [DataField]
    public TimeSpan HealInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Доля текущего урона группы, которая снимается за одну порцию.
    /// </summary>
    [DataField]
    public float HealFraction = 0.05f;

    /// <summary>
    /// Минимальное лечение за порцию, чтобы эффект работал и при лёгких ранах.
    /// </summary>
    [DataField]
    public FixedPoint2 MinHeal = 0.25f;

    /// <summary>
    /// Максимальное лечение за порцию, чтобы тяжёлые раны не заживали мгновенно.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxHeal = 5f;

    /// <summary>
    /// Момент следующей порции лечения.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextHeal;
}
