using Content.Server._Aquila.Chat;
using Content.Shared._Starlight.CollectiveMind;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private bool CanReceiveCollectiveMind(EntityUid source, EntityUid listener, CollectiveMindPrototype channel)
    {
        var ev = new CollectiveMindReceiveAttemptEvent(source, channel.ID);
        RaiseLocalEvent(listener, ref ev);
        return !ev.Cancelled;
    }
}
