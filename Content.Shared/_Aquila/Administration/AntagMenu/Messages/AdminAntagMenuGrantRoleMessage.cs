using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Просьба выдать разуму антагонистскую роль.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuGrantRoleMessage(NetEntity mind, string antag) : EuiMessageBase
{
    /// <summary>
    /// Разум, которому выдаётся роль.
    /// </summary>
    public readonly NetEntity Mind = mind;

    /// <summary>
    /// Прототип антагониста.
    /// </summary>
    public readonly string Antag = antag;
}
