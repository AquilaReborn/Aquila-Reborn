using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration.AntagMenu.Messages;

/// <summary>
/// Сообщает окну об ошибке, ключ локализации передаётся клиенту.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminAntagMenuErrorMessage(string messageKey) : EuiMessageBase
{
    /// <summary>
    /// Ключ локализации с текстом ошибки.
    /// </summary>
    public readonly string MessageKey = messageKey;
}
