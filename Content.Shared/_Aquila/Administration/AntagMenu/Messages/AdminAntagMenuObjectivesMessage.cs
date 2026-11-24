using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Ответ со списком целей, которые можно выдать разуму.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuObjectivesMessage(NetEntity mind, List<string> prototypes) : EuiMessageBase
{
    /// <summary>
    /// Разум, для которого собраны цели.
    /// </summary>
    public readonly NetEntity Mind = mind;

    /// <summary>
    /// Прототипы целей, которые можно выдать.
    /// </summary>
    public readonly List<string> Prototypes = prototypes;
}
