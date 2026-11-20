using Content.Client.Replay.Spectator;
using Robust.Client.Replays.Playback;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Client.Examine;

public sealed partial class ExamineSystem
{
    [Dependency] private readonly INetManager _examineConnection = default!;
    [Dependency] private readonly IReplayPlaybackManager _examineReplay = default!;

    /// <summary>
    /// Checks whether the client may ask the server for extra examine information about the given entity.
    /// </summary>
    /// <param name="entity">Entity that is going to be examined.</param>
    /// <param name="meta">Metadata of the entity, if the caller has already resolved it.</param>
    /// <returns><see langword="true"/> if the client is connected, is not replaying and the entity is not client-side.</returns>
    public bool CanRequestExamineAquila(EntityUid entity, MetaDataComponent? meta = null)
    {
        return _examineConnection.IsConnected && _examineReplay.Replay == null && !IsClientSide(entity, meta);
    }

    private bool CanReplayExamineAquila(EntityUid examiner, MapCoordinates target)
    {
        return _examineReplay.Replay != null && HasComp<ReplaySpectatorComponent>(examiner) &&
            target.MapId == Transform(examiner).MapID && _eyeManager.GetWorldViewbounds().Contains(target.Position);
    }
}
