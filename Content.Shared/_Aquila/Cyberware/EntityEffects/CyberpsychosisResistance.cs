using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.EntityEffects;

/// <summary>
/// На время снижает шанс, что киберпсихоз начнётся или перейдёт на стадию тяжелее.
/// </summary>
public sealed partial class CyberpsychosisResistance : EntityEffectBase<CyberpsychosisResistance>
{
    /// <summary>
    /// Сколько после срабатывания держится защита.
    /// </summary>
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Во сколько раз умножается шанс перехода на следующую стадию, пока действует защита.
    /// </summary>
    [DataField]
    public float Multiplier = 0.5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-cyberpsychosis-resistance",
            ("chance", Probability),
            ("percent", MathF.Round((1f - Multiplier) * 100f)),
            ("time", Time.TotalSeconds));
}
