using Content.Goobstation.Maths.FixedPoint;

namespace Content.Shared.Heretic;

public sealed partial class HereticCombatMarkComponent
{
    /// <summary>
    /// Урон мозгу, который наносит метка Луны.
    /// </summary>
    [DataField]
    public FixedPoint2 MoonBrainDamage = 25;

    /// <summary>
    /// Длительность галлюцинаций, которые накладывает метка Луны.
    /// </summary>
    [DataField]
    public TimeSpan MoonInsanityDuration = TimeSpan.FromSeconds(30);
}
