using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.EntityEffects;

/// <summary>
/// Продвигает курс лечения киберпсихоза. Работает, только пока кибернагрузка ниже порога.
/// </summary>
public sealed partial class CyberpsychosisTherapy : EntityEffectBase<CyberpsychosisTherapy>
{
    /// <summary>
    /// Сколько секунд курса засчитывается за одно срабатывание.
    /// </summary>
    [DataField]
    public float Amount = 1f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-cyberpsychosis-therapy", ("chance", Probability));
}
