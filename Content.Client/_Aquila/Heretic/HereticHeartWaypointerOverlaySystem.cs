using Robust.Client.Graphics;

namespace Content.Client._Aquila.Heretic;

/// <summary>
/// Подключает оверлей стрелки живого сердца.
/// </summary>
public sealed class HereticHeartWaypointerOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private HereticHeartWaypointerOverlay _heartOverlay = default!;

    /// <summary>
    /// Подключает оверлей стрелки при инициализации системы.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _heartOverlay = new HereticHeartWaypointerOverlay();
        _overlay.AddOverlay(_heartOverlay);
    }

    /// <summary>
    /// Снимает оверлей стрелки при остановке системы.
    /// </summary>
    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_heartOverlay);
    }
}
