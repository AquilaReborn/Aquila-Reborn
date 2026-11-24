using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Запрос на повторную сборку состояния меню.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuRefreshMessage : EuiMessageBase;
