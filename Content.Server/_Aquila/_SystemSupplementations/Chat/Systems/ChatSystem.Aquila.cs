using Content.Shared.Chat;
using Robust.Shared.Network;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private bool TrySendUnclearMessage(ChatChannel channel, EntityUid listener, EntityUid source, bool hideChat, INetChannel client)
    {
        if (channel != ChatChannel.Local && channel != ChatChannel.Whisper)
            return false;

        if (!_mobStateSystem.IsCritical(listener) && !_mobStateSystem.IsDead(listener))
            return false;

        var unclear = Loc.GetString("chat-crit-unclear");
        _chatManager.ChatMessageToOne(channel, unclear, unclear, source, hideChat, client);
        return true;
    }
}
