using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Просьба удалить цель разума.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuRemoveObjectiveMessage(NetEntity mind, NetEntity objective) : EuiMessageBase
{
    /// <summary>
    /// Разум, у которого удаляется цель.
    /// </summary>
    public readonly NetEntity Mind = mind;

    /// <summary>
    /// Удаляемая цель.
    /// </summary>
    public readonly NetEntity Objective = objective;
}
