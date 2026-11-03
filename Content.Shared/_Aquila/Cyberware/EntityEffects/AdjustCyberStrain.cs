using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.EntityEffects;

/// <summary>
/// Меняет напряжение от свежих установок имплантов. Отрицательное значение ускоряет адаптацию.
/// </summary>
public sealed partial class AdjustCyberStrain : EntityEffectBase<AdjustCyberStrain>
{
    /// <summary>
    /// На сколько изменить напряжение за одно срабатывание.
    /// </summary>
    [DataField]
    public float Amount;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-adjust-cyber-strain",
            ("chance", Probability),
            ("deltasign", MathF.Sign(Amount)));
}
