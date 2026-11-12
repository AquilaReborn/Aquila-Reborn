using Robust.Client.Replays.Playback;

namespace Content.Client.Verbs.UI;

public sealed partial class VerbMenuUIController
{
    [Dependency] private readonly IReplayPlaybackManager _verbMenuReplay = default!;

    private bool IsCombatMenuBlockedAquila(EntityUid user)
    {
        return _verbMenuReplay.Replay == null && _combatMode.IsInCombatMode(user);
    }
}
