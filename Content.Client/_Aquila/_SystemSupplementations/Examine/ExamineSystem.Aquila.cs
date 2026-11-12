using Content.Client.Replay.Spectator;
using Robust.Client.Replays.Playback;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Client.Examine;

public sealed partial class ExamineSystem
{
    [Dependency] private readonly INetManager _examineConnection = default!;
    [Dependency] private readonly IReplayPlaybackManager _examineReplay = default!;

    private bool CanRequestExamineAquila(EntityUid entity)
    {
        return _examineConnection.IsConnected && _examineReplay.Replay == null && !IsClientSide(entity);
    }

    private bool CanReplayExamineAquila(EntityUid examiner, MapCoordinates target)
    {
        return _examineReplay.Replay != null && HasComp<ReplaySpectatorComponent>(examiner) &&
            target.MapId == Transform(examiner).MapID && _eyeManager.GetWorldViewbounds().Contains(target.Position);
    }
}
