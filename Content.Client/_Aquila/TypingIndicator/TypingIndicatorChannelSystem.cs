using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared._Aquila.TypingIndicator;
using Content.Shared._Aquila.TypingIndicator.Systems;
using Robust.Client.Player;
using Robust.Shared.Configuration;

namespace Content.Client._Aquila.TypingIndicator;

public sealed class TypingIndicatorChannelSystem : SharedTypingIndicatorChannelSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    /// <summary>
    /// Сообщает серверу, в какой канал чата печатает игрок, если от этого зависит индикатор над персонажем.
    /// </summary>
    /// <param name="channel">Канал, в который сейчас печатает игрок.</param>
    public void ClientChangedChatChannel(ChatSelectChannel channel)
    {
        if (!_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            return;

        if (_player.LocalEntity is not { } uid)
            return;

        if (GetChannelIndicator(uid, channel) == GetCurrentIndicator(uid))
            return;

        RaisePredictiveEvent(new TypingChannelChangedEvent(channel));
    }
}
