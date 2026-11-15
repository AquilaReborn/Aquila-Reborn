using Content.Client.Replay.Spectator;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.Replays.Playback;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Client.Examine;

public sealed partial class ExamineSystem
{
    [Dependency] private readonly INetManager _examineConnection = default!;
    [Dependency] private readonly IReplayPlaybackManager _examineReplay = default!;
    [Dependency] private readonly MobStateSystem _examineMobState = default!;

    private bool CanRequestExamineAquila(EntityUid entity)
    {
        return _examineConnection.IsConnected && _examineReplay.Replay == null && !IsClientSide(entity);
    }

    private bool CanReplayExamineAquila(EntityUid examiner, MapCoordinates target)
    {
        return _examineReplay.Replay != null && HasComp<ReplaySpectatorComponent>(examiner) &&
            target.MapId == Transform(examiner).MapID && _eyeManager.GetWorldViewbounds().Contains(target.Position);
    }

    private bool IsExamineBlockedAquila(EntityUid examiner, EntityUid? examined)
    {
        if (examined is not { } target || !HasComp<MobStateComponent>(target))
            return false;

        return _examineMobState.IsCritical(examiner) || _examineMobState.IsDead(examiner);
    }
}
