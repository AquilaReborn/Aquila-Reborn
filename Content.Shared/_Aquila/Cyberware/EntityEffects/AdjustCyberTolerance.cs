using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.EntityEffects;

/// <summary>
/// Навсегда повышает переносимость имплантов: риск киберпсихоза считается ниже на эту величину. Ограничено потолком тела.
/// </summary>
public sealed partial class AdjustCyberTolerance : EntityEffectBase<AdjustCyberTolerance>
{
    /// <summary>
    /// На сколько повысить переносимость за одно срабатывание.
    /// </summary>
    [DataField]
    public float Amount;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-adjust-cyber-tolerance", ("chance", Probability));
}
