using Content.Shared.Chat;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.TypingIndicator;

/// <summary>
/// Отправляется клиентом на сервер, когда игрок начинает печатать в другой канал чата.
/// </summary>
[Serializable, NetSerializable]
public sealed class TypingChannelChangedEvent : EntityEventArgs
{
    /// <summary>
    /// Канал чата, в который печатает игрок.
    /// </summary>
    public readonly ChatSelectChannel Channel;

    public TypingChannelChangedEvent(ChatSelectChannel channel)
    {
        Channel = channel;
    }
}
