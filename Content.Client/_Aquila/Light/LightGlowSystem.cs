using Content.Shared._Aquila.CCVars;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client._Aquila.Light;

/// <summary>
/// Подключает оверлей свечения светильников и следит за его настройками.
/// </summary>
public sealed class LightGlowSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private LightGlowOverlay _glowOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _glowOverlay = new LightGlowOverlay();
        Subs.CVar(_cfg, AquilaCVars.LightGlow, OnEnabledChanged, true);
        Subs.CVar(_cfg, AquilaCVars.LightGlowIntensity, OnIntensityChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay(_glowOverlay);
    }

    private void OnEnabledChanged(bool enabled)
    {
        if (enabled)
            _overlay.AddOverlay(_glowOverlay);
        else
            _overlay.RemoveOverlay(_glowOverlay);
    }

    private void OnIntensityChanged(float intensity)
    {
        _glowOverlay.Intensity = Math.Clamp(intensity, 0f, 1f);
    }
}
