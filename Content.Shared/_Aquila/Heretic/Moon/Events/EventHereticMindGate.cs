using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Heretic.Moon.Events;

/// <summary>
/// Разрывает рассудок цели: немота, глухота, слепота, галлюцинации и урон мозгу. Кастующий тоже страдает.
/// </summary>
public sealed partial class EventHereticMindGate : EntityTargetActionEvent
{
    /// <summary>
    /// Урон мозгу, который получает кастующий.
    /// </summary>
    [DataField]
    public FixedPoint2 SelfDamage = 20;

    /// <summary>
    /// Урон мозгу, который получает цель.
    /// </summary>
    [DataField]
    public FixedPoint2 TargetDamage = 30;

    /// <summary>
    /// Длительность слепоты, немоты, глухоты и галлюцинаций.
    /// </summary>
    [DataField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Прототип статус-эффекта, накладываемого на цель.
    /// </summary>
    [DataField]
    public EntProtoId StatusEffect = "StatusEffectMoonMindGate";

    /// <summary>
    /// Прототип разового визуального эффекта на цели.
    /// </summary>
    [DataField]
    public EntProtoId Effect = "EffectMoonInsanity";

    /// <summary>
    /// Прототип второго визуального эффекта, который вспыхивает на цели.
    /// </summary>
    [DataField]
    public EntProtoId BurstEffect = "EffectMoonRingleader";

    /// <summary>
    /// Звук, который слышат все вокруг цели.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("MoonAmuletLaugh");
}
