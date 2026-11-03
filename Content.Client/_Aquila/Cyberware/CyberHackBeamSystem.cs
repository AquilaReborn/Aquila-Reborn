using Robust.Client.Graphics;

namespace Content.Client._Aquila.Cyberware;

/// <summary>
/// Подключает оверлей полотна взлома.
/// </summary>
public sealed class CyberHackBeamSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private CyberHackBeamOverlay _beamOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _beamOverlay = new CyberHackBeamOverlay();
        _overlay.AddOverlay(_beamOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_beamOverlay);
    }
}
