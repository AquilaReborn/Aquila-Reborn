using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Heretic.Moon.Components;

/// <summary>
/// Снаряд Лунного парада: пронзает всех на пути и накладывает безумие.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class MoonParadeProjectileComponent : Component
{
    private static readonly EntProtoId DefaultFractureStatus = "StatusEffectMoonParadeFracture";

    /// <summary>
    /// Урон мозгу, который получает каждая жертва.
    /// </summary>
    [DataField]
    public FixedPoint2 BrainDamage = 10;

    /// <summary>
    /// Длительность галлюцинаций у жертвы. Должна перекрывать интервал принудительного смеха.
    /// </summary>
    [DataField]
    public TimeSpan HallucinationDuration = TimeSpan.FromSeconds(12);

    /// <summary>
    /// Длительность помутнения рассудка у жертвы.
    /// </summary>
    [DataField]
    public TimeSpan ConfusionDuration = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Статус зеркального раскола экрана при попадании.
    /// </summary>
    [DataField]
    public EntProtoId FractureStatus = DefaultFractureStatus;

    /// <summary>
    /// Длительность зеркального раскола экрана жертвы.
    /// </summary>
    [DataField]
    public TimeSpan FractureDuration = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Прототип визуального эффекта на жертве.
    /// </summary>
    [DataField]
    public EntProtoId Effect = "EffectMoonParadeImpact";
}
