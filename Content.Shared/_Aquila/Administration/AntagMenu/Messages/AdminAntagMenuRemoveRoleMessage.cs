using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Просьба снять с разума антагонистскую роль.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuRemoveRoleMessage(NetEntity mind, string antag) : EuiMessageBase
{
    /// <summary>
    /// Разум, с которого снимается роль.
    /// </summary>
    public readonly NetEntity Mind = mind;

    /// <summary>
    /// Прототип антагониста.
    /// </summary>
    public readonly string Antag = antag;
}
