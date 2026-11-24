using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Просьба удалить все цели разума.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuRemoveAllObjectivesMessage(NetEntity mind) : EuiMessageBase
{
    /// <summary>
    /// Разум, у которого удаляются цели.
    /// </summary>
    public readonly NetEntity Mind = mind;
}
