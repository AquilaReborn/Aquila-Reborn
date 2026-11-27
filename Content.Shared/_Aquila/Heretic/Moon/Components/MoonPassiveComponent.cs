using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Пассивная сила пути Луны: постоянная регенерация мозга и иммунитет ко сну.
/// </summary>
[RegisterComponent, Access(typeof(SharedMoonPassiveSystem)), AutoGenerateComponentPause]
public sealed partial class MoonPassiveComponent : Component
{
    /// <summary>
    /// Как часто восстанавливается мозг.
    /// </summary>
    [DataField]
    public TimeSpan HealInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Сколько урона мозгу восстанавливается за один тик вне боя.
    /// </summary>
    [DataField]
    public FixedPoint2 HealAmount = 1.5;

    /// <summary>
    /// Множитель восстановления мозга, пока носитель недавно получал урон.
    /// </summary>
    [DataField]
    public float CombatHealMultiplier = 0.5f;

    /// <summary>
    /// Сколько времени после получения урона носитель считается находящимся в бою.
    /// </summary>
    [DataField]
    public TimeSpan CombatTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Как часто снимаются эффекты сна.
    /// </summary>
    [DataField]
    public TimeSpan SleepImmunityInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Время следующего восстановления мозга.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextHeal;

    /// <summary>
    /// Время следующего снятия эффектов сна.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSleepImmunity;

    /// <summary>
    /// Время, до которого носитель считается находящимся в бою.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan CombatEndTime;
}
