using Content.Shared.EntityEffects;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.EntityEffects;

/// <summary>
/// Приступ ярости киберпсихопата: тело само выхватывает оружие и нападает на ближайших.
/// </summary>
public sealed partial class CyberpsychosisRampage : EntityEffectBase<CyberpsychosisRampage>
{
    private static readonly ProtoId<NpcFactionPrototype> AllHostileFaction = "AllHostile";

    /// <summary>
    /// Сколько длится приступ.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Фракция, к которой больной принадлежит на время приступа.
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = AllHostileFaction;

    /// <summary>
    /// Компоненты, которые управляют телом во время приступа, обычно ИИ нападения.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;
}
