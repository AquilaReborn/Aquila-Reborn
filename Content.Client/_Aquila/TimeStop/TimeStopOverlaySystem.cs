using Robust.Client.Graphics;

namespace Content.Client.Aquila.TimeStop;

public sealed class TimeStopOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private TimeStopOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new TimeStopOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlay != null)
            _overlayMan.RemoveOverlay(_overlay);
    }
}
