using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.EntityEffects;

/// <summary>
/// Глушит приступы киберпсихоза на время: больной не срывается, не видит враждебных обликов и не уходит на стадию тяжелее.
/// </summary>
public sealed partial class SuppressCyberpsychosis : EntityEffectBase<SuppressCyberpsychosis>
{
    /// <summary>
    /// На сколько продлевается затишье за одно срабатывание.
    /// </summary>
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(5);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-suppress-cyberpsychosis", ("chance", Probability));
}
