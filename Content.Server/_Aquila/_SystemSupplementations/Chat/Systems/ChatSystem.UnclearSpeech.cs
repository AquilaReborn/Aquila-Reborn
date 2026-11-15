using Content.Shared.Chat;
using Robust.Shared.Network;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private bool TrySendUnclearMessage(ChatChannel channel, EntityUid listener, EntityUid source, bool hideChat, INetChannel client)
    {
        if (listener == source)
            return false;

        if (!_mobStateSystem.IsCritical(listener) && !_mobStateSystem.IsDead(listener))
            return false;

        if (channel == ChatChannel.Emotes)
            return true;

        if (channel != ChatChannel.Local && channel != ChatChannel.Whisper)
            return false;

        var unclear = Loc.GetString("chat-crit-unclear");
        _chatManager.ChatMessageToOne(channel, unclear, unclear, default, hideChat, client);
        return true;
    }
}
