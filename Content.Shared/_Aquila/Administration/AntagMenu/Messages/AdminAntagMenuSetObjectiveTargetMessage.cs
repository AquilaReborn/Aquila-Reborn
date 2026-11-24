using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Просьба задать требуемое значение числовой цели.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuSetObjectiveTargetMessage(NetEntity mind, NetEntity objective, int value) : EuiMessageBase
{
    /// <summary>
    /// Разум, которому принадлежит цель.
    /// </summary>
    public readonly NetEntity Mind = mind;

    /// <summary>
    /// Изменяемая цель.
    /// </summary>
    public readonly NetEntity Objective = objective;

    /// <summary>
    /// Новое требуемое значение.
    /// </summary>
    public readonly int Value = value;
}
