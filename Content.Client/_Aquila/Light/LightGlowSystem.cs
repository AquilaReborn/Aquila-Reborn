using Robust.Client.Graphics;

namespace Content.Client._Aquila.Light;

/// <summary>
/// Подключает оверлей свечения светильников.
/// </summary>
public sealed class LightGlowSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new LightGlowOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<LightGlowOverlay>();
    }
}
