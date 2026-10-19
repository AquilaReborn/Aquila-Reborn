using Content.Shared.Aquila.Spin;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;

namespace Content.Client.Aquila.Spin;

public sealed class SpinUserSystem : SharedSpinSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new SpinUserOverlay(EntityManager, _player, _resourceCache));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<SpinUserOverlay>();
    }
}
