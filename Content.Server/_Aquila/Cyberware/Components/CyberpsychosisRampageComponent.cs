using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Cyberware.Components;

/// <summary>
/// Идёт приступ ярости: телом управляет ИИ нападения.
/// </summary>
[RegisterComponent, Access(typeof(CyberpsychosisRampageSystem)), AutoGenerateComponentPause]
public sealed partial class CyberpsychosisRampageComponent : Component
{
    /// <summary>
    /// Когда приступ закончится.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Фракции, которые были у больного до приступа.
    /// </summary>
    [ViewVariables]
    public HashSet<ProtoId<NpcFactionPrototype>> SavedFactions = new();

    /// <summary>
    /// Компоненты, которые добавил приступ и которые нужно снять после него.
    /// </summary>
    [ViewVariables]
    public List<string> AddedComponents = new();
}
