using Content.Goobstation.Maths.FixedPoint;

namespace Content.Shared.Heretic.Components;

public sealed partial class HereticBladeComponent
{
    /// <summary>
    /// Длительность галлюцинаций, которые накладывает лунный клинок.
    /// </summary>
    [DataField]
    public TimeSpan MoonInsanityDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Урон мозгу, который наносит лунный клинок.
    /// </summary>
    [DataField]
    public FixedPoint2 MoonBrainDamage = 10;

    /// <summary>
    /// Урон мозгу, который лунный клинок наносит безумной или лежачей жертве.
    /// </summary>
    [DataField]
    public FixedPoint2 MoonInsaneBrainDamage = 25;

    /// <summary>
    /// Порог урона мозгу, после которого жертва считается безумной для усиленного удара лунного клинка.
    /// </summary>
    [DataField]
    public float MoonInsanityThreshold = 50f;
}
