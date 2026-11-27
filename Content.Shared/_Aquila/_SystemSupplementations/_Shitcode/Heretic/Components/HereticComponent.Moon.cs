using Content.Goobstation.Maths.FixedPoint;

namespace Content.Shared.Heretic;

public sealed partial class HereticComponent
{
    /// <summary>
    /// Урон мозгу, который наносит Хват Мансуса пути Луны.
    /// </summary>
    [DataField]
    public FixedPoint2 MoonGraspBrainDamage = 10;

    /// <summary>
    /// Длительность галлюцинаций, которые накладывает Хват Мансуса пути Луны.
    /// </summary>
    [DataField]
    public TimeSpan MoonGraspInsanityDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Длительность сокрытия личности после Хвата Мансуса пути Луны.
    /// </summary>
    [DataField]
    public TimeSpan MoonGraspHideDuration = TimeSpan.FromSeconds(15);
}
