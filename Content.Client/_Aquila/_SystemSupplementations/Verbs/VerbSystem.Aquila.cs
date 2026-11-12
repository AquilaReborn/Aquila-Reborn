using Content.Shared.Verbs;
using Robust.Client.Replays.Playback;
using Robust.Shared.Network;

namespace Content.Client.Verbs;

public sealed partial class VerbSystem
{
    [Dependency] private readonly INetManager _verbConnection = default!;
    [Dependency] private readonly IReplayPlaybackManager _verbReplay = default!;

    private bool CanSendServerVerbsAquila(NetEntity target)
    {
        return _verbConnection.IsConnected && _verbReplay.Replay == null && !target.IsClientSide();
    }

    private SortedSet<Verb> GetMenuVerbsAquila(EntityUid local, NetEntity target, EntityUid user, List<Type> verbTypes,
        out List<VerbCategory> extraCategories, bool force)
    {
        var verbs = GetLocalVerbs(local, user, verbTypes, out extraCategories, force);
        if (!target.IsClientSide() && !CanSendServerVerbsAquila(target))
            verbs.RemoveWhere(verb => !verb.ClientExclusive);

        return verbs;
    }
}
