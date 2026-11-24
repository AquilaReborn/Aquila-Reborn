using Content.Client._Aquila.Heretic.Components;
using Content.Client._Aquila.Heretic.Helpers;
using Content.Client._Aquila.Heretic.Overlays;
using Content.Shared._Aquila.Heretic;
using Content.Shared._Aquila.Heretic.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.Heretic.Systems;

/// <summary>
/// Показывает и плавно гасит эффекты Мансуса у местного игрока: у жертвы испытания это дымка, щупальца и чёрная пелена,
/// у еретика при срабатывании зрения сдвига реальности только дымка и щупальца.
/// </summary>
public sealed class MansusRealmOverlaySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private static readonly TimeSpan RealmFadeInDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan VisionFadeInDuration = TimeSpan.FromSeconds(0.9);
    private static readonly TimeSpan VisionFadeOutDuration = TimeSpan.FromSeconds(0.9);

    private MansusRealmOverlay _realmOverlay = default!;
    private MansusHandArmOverlay _armOverlay = default!;
    private MansusFadeOverlay _fadeOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _realmOverlay = new MansusRealmOverlay();
        _armOverlay = new MansusHandArmOverlay();
        _fadeOverlay = new MansusFadeOverlay();
        _overlay.AddOverlay(_armOverlay);

        SubscribeNetworkEvent<RealityShiftVisionStartedEvent>(OnVisionStarted);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_realmOverlay);
        _overlay.RemoveOverlay(_armOverlay);
        _overlay.RemoveOverlay(_fadeOverlay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } local)
        {
            Hide();
            return;
        }

        var now = _timing.CurTime;

        if (TryComp<MansusVictimComponent>(local, out var victim))
        {
            _armOverlay.Intensity = 1f;
            _realmOverlay.Intensity = MansusFadeHelper.GetAlpha(now, victim.ArrivalTime, RealmFadeInDuration);
            Show(true);
            return;
        }

        if (TryComp<RealityShiftVisionVisualsComponent>(local, out var visuals))
        {
            var alpha = MansusFadeHelper.GetAlpha(now, visuals.StartTime, VisionFadeInDuration, visuals.EndTime, VisionFadeOutDuration);
            _armOverlay.Intensity = alpha;
            _realmOverlay.Intensity = alpha;
            Show(false);

            if (now >= visuals.EndTime + VisionFadeOutDuration)
                RemCompDeferred(local, visuals);

            return;
        }

        Hide();
    }

    private void OnVisionStarted(RealityShiftVisionStartedEvent args)
    {
        if (_player.LocalEntity is not { } local)
            return;

        var visuals = EnsureComp<RealityShiftVisionVisualsComponent>(local);
        visuals.StartTime = _timing.CurTime;
        visuals.EndTime = visuals.StartTime + args.Duration;
    }

    private void Show(bool withCurtain)
    {
        _overlay.AddOverlay(_realmOverlay);
        if (withCurtain)
        {
            _overlay.AddOverlay(_fadeOverlay);
            return;
        }

        if (_overlay.HasOverlay<MansusFadeOverlay>())
            _overlay.RemoveOverlay(_fadeOverlay);
    }

    private void Hide()
    {
        _realmOverlay.Intensity = 0f;
        _armOverlay.Intensity = 1f;

        if (_overlay.HasOverlay<MansusRealmOverlay>())
            _overlay.RemoveOverlay(_realmOverlay);

        if (_overlay.HasOverlay<MansusFadeOverlay>())
            _overlay.RemoveOverlay(_fadeOverlay);
    }
}
