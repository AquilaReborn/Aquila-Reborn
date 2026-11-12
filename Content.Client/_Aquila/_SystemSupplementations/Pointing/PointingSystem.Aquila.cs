using Content.Shared.Pointing;
using Robust.Client.Replays.Playback;
using Robust.Shared.Network;

namespace Content.Client.Pointing;

public sealed partial class PointingSystem
{
    [Dependency] private readonly INetManager _pointingConnection = default!;
    [Dependency] private readonly IReplayPlaybackManager _pointingReplay = default!;

    private bool CanSendPointingVerbAquila(EntityUid target)
    {
        return _pointingConnection.IsConnected && _pointingReplay.Replay == null && !IsClientSide(target);
    }

    private void SendPointingVerbAquila(EntityUid target)
    {
        if (Exists(target) && CanSendPointingVerbAquila(target))
            RaiseNetworkEvent(new PointingAttemptEvent(GetNetEntity(target)));
    }
}
