using Content.Goobstation.Shared.Aquila.ErasureTime;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Aquila.ErasureTime;

public sealed class ErasureTimeWaveOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    private ErasureTimeWaveOverlay? _overlayInstance;

    public (TimeSpan Start, float Duration)? ActiveWave { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        _overlayInstance = new ErasureTimeWaveOverlay(this);
        _overlay.AddOverlay(_overlayInstance);

        SubscribeNetworkEvent<ErasureTimeVisualEffectEvent>(OnVisualEffect);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlayInstance != null)
            _overlay.RemoveOverlay(_overlayInstance);
    }

    private void OnVisualEffect(ErasureTimeVisualEffectEvent ev)
    {
        if (_player.LocalEntity is not { } local)
            return;

        var source = _entManager.GetEntity(ev.Source);
        if (!_entManager.TryGetComponent(source, out TransformComponent? sourceXform)
            || !_entManager.TryGetComponent(local, out TransformComponent? localXform))
            return;

        var sourcePos = _entManager.System<SharedTransformSystem>().GetWorldPosition(sourceXform);
        var localPos = _entManager.System<SharedTransformSystem>().GetWorldPosition(localXform);

        if ((sourcePos - localPos).Length() > ev.Radius)
            return;

        ActiveWave = (_timing.CurTime, ev.WaveDuration);
    }

    public float GetProgress()
    {
        if (ActiveWave is not { } wave)
            return 0f;

        var elapsed = (float) (_timing.CurTime - wave.Start).TotalSeconds;
        if (elapsed >= wave.Duration)
        {
            ActiveWave = null;
            return 0f;
        }

        return elapsed / wave.Duration;
    }
}
