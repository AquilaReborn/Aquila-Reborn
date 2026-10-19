using Content.Shared.Aquila.TimeRewind;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Aquila.TimeRewind;

public sealed class RewindOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly RewindOverlay _overlay = new();
    private bool _active;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var localEnt = _player.LocalEntity;

        var shouldShow = localEnt != null
            && TryComp<RewindedComponent>(localEnt.Value, out var rewinded)
            && rewinded.EndTime > _timing.CurTime;

        if (shouldShow == _active)
            return;

        _active = shouldShow;

        if (_active)
            _overlayMan.AddOverlay(_overlay);
        else
            _overlayMan.RemoveOverlay(_overlay);
    }
}
