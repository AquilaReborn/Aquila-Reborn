using Robust.Client.Graphics;

namespace Content.Client.Aquila.Overlays;

public sealed partial class VisionOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private VisionOverlay? _overlay;

    /// <summary>
    /// Добавляет оверлей ограничения обзора.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        _overlay = new VisionOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    /// <summary>
    /// Убирает оверлей ограничения обзора.
    /// </summary>
    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlay != null)
            _overlayMan.RemoveOverlay(_overlay);
    }
}
