using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Просьба выдать разуму цель из прототипа.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuAddObjectiveMessage(NetEntity mind, string prototype) : EuiMessageBase
{
    /// <summary>
    /// Разум, которому выдаётся цель.
    /// </summary>
    public readonly NetEntity Mind = mind;

    /// <summary>
    /// Прототип цели.
    /// </summary>
    public readonly string Prototype = prototype;
}
