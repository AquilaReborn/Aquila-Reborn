using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Просьба прислать список целей, которые подходят выбранному разуму.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuRequestObjectivesMessage(NetEntity mind) : EuiMessageBase
{
    /// <summary>
    /// Разум, для которого нужно проверить цели.
    /// </summary>
    public readonly NetEntity Mind = mind;
}
