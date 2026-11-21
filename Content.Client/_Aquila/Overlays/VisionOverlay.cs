using System.Numerics;
using Content.Shared._Aquila.PreCritical;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Aquila.Overlays;

public sealed class VisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public override bool RequestScreenTexture => true;

    private const float PreCriticalRadiusMax = 0.34f;
    private const float PreCriticalRadiusMin = 0.16f;
    private const float DeadRadius = 0.08f;
    private const float RadiusSmoothSpeed = 8f;
    private const float FadeSpeed = 8f;
    private const float FadeThreshold = 0.01f;
    private const float MaxDelta = 0.1f;
    private const float PulseSpeed = 3f;
    private const float PreCriticalSeverityStart = 0.75f;
    private const float PreCriticalSeverityRange = 0.25f;
    private const float PreCriticalPulseAmplitude = 0.12f;
    private const float VignetteInnerRatio = 0.3f;
    private const float VignetteOuterRatio = 1.05f;
    private const float HazeInnerRadius = 0.6f;
    private const float HazeInnerRadiusShrink = 0.58f;
    private const float HazeOuterRadius = 2f;
    private const float HazeAlpha = 0.3f;
    private const float HazeInnerFeather = 0.005f;
    private const float HazeOuterFeather = 0.2f;
    private const float RedInnerRadiusMin = 0.55f;
    private const float RedInnerRadiusBeat = 0.15f;
    private const float RedOuterRadiusMin = 1.15f;
    private const float RedOuterRadiusBeat = 0.35f;
    private const float RedAlpha = 0.12f;
    private const float RedAlphaMin = 0.35f;
    private const float RedAlphaBeat = 0.65f;

    private static readonly ProtoId<ShaderPrototype> MaskShader = "GradientCircleMask";
    private static readonly ProtoId<ShaderPrototype> GrayscaleShader = "GreyscaleFullscreen";

    private readonly ShaderInstance _mask;
    private readonly ShaderInstance _red;
    private readonly ShaderInstance _haze;
    private readonly ShaderInstance _grayscale;

    private float _smoothedRadius;
    private float _smoothedSeverity;
    private float _fade;
    private bool _wasActive;
    private bool _justActivated;
    private bool _dead;
    private bool _critical;
    private bool _preCritical;
    private TimeSpan _lastUpdate;

    public VisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _mask = _proto.Index(MaskShader).InstanceUnique();
        _red = _proto.Index(MaskShader).InstanceUnique();
        _haze = _proto.Index(MaskShader).InstanceUnique();
        _grayscale = _proto.Index(GrayscaleShader).InstanceUnique();
        ZIndex = 50;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var hasState = TryGetState(out _dead, out _critical, out _preCritical);

        if (hasState)
        {
            if (!_wasActive)
            {
                _wasActive = true;
                _justActivated = true;
                _lastUpdate = _timing.RealTime;
            }

            return true;
        }

        _wasActive = false;

        if (_fade <= FadeThreshold)
        {
            ResetState();
            return false;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        if (player == null)
        {
            ResetState();
            return;
        }

        var handle = args.WorldHandle;
        var box = args.WorldBounds;
        var view = args.ViewportBounds;
        var distance = MathF.Min(view.Width, view.Height);

        var delta = Math.Clamp((float) (_timing.RealTime - _lastUpdate).TotalSeconds, 0f, MaxDelta);
        _lastUpdate = _timing.RealTime;
        var fadeTarget = _dead || _critical || _preCritical ? 1f : 0f;
        _fade = Smooth(_fade, fadeTarget, FadeSpeed, delta);

        if (_critical)
        {
            var severity = GetCriticalSeverity(player.Value);

            if (_justActivated)
            {
                _smoothedSeverity = severity;
                _justActivated = false;
            }
            else
            {
                _smoothedSeverity = Smooth(_smoothedSeverity, severity, RadiusSmoothSpeed, delta);
            }

            var critBeat = 0.5f + 0.5f * MathF.Sin((float) _timing.RealTime.TotalSeconds * PulseSpeed);

            var darkRadius = distance * (PreCriticalRadiusMin + (DeadRadius - PreCriticalRadiusMin) * _smoothedSeverity);
            _smoothedRadius = darkRadius;

            _mask.SetParameter("time", 0f);
            _mask.SetParameter("color", new Vector3(0f, 0f, 0f));
            _mask.SetParameter("darknessAlphaOuter", _fade);
            _mask.SetParameter("innerCircleRadius", darkRadius * VignetteInnerRatio);
            _mask.SetParameter("innerCircleMaxRadius", darkRadius * VignetteInnerRatio);
            _mask.SetParameter("outerCircleRadius", darkRadius * VignetteOuterRatio);
            _mask.SetParameter("outerCircleMaxRadius", darkRadius * VignetteOuterRatio);
            handle.UseShader(_mask);
            handle.DrawRect(box, Color.White);

            var hazeInner = distance * (HazeInnerRadius - HazeInnerRadiusShrink * _smoothedSeverity);
            var hazeOuter = distance * (HazeOuterRadius - _smoothedSeverity);

            _haze.SetParameter("time", critBeat);
            _haze.SetParameter("color", Vector3.One);
            _haze.SetParameter("darknessAlphaOuter", HazeAlpha * _fade);
            _haze.SetParameter("innerCircleRadius", hazeInner);
            _haze.SetParameter("innerCircleMaxRadius", hazeInner + HazeInnerFeather * distance);
            _haze.SetParameter("outerCircleRadius", hazeOuter);
            _haze.SetParameter("outerCircleMaxRadius", hazeOuter + HazeOuterFeather * distance);
            handle.UseShader(_haze);
            handle.DrawRect(box, Color.White);
            handle.UseShader(null);
            return;
        }

        if (_dead || _preCritical)
        {
            var targetRadius = _dead ? distance * DeadRadius : GetPreCriticalRadius(player.Value, distance);

            if (_justActivated || (_dead && _smoothedRadius <= 0f))
            {
                _smoothedRadius = targetRadius;
                _justActivated = false;
            }
            else
            {
                _smoothedRadius = Smooth(_smoothedRadius, targetRadius, RadiusSmoothSpeed, delta);
            }
        }

        var radius = _smoothedRadius;
        var beat = 0.5f + 0.5f * MathF.Sin((float) _timing.RealTime.TotalSeconds * PulseSpeed);

        if (_dead && ScreenTexture != null)
        {
            _grayscale.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            handle.UseShader(_grayscale);
            handle.DrawRect(box, Color.White);
        }

        if (_preCritical)
        {
            var maskRadius = radius * (1f + PreCriticalPulseAmplitude * (beat - 0.5f));
            var redInner = maskRadius * (RedInnerRadiusMin + RedInnerRadiusBeat * beat);
            var redOuter = maskRadius * (RedOuterRadiusMin + RedOuterRadiusBeat * beat);
            _red.SetParameter("time", 0f);
            _red.SetParameter("color", new Vector3(1f, 0f, 0f));
            _red.SetParameter("darknessAlphaOuter", RedAlpha * (RedAlphaMin + RedAlphaBeat * beat) * _fade);
            _red.SetParameter("innerCircleRadius", redInner);
            _red.SetParameter("innerCircleMaxRadius", redInner);
            _red.SetParameter("outerCircleRadius", redOuter);
            _red.SetParameter("outerCircleMaxRadius", redOuter);
            handle.UseShader(_red);
            handle.DrawRect(box, Color.White);
        }

        _mask.SetParameter("time", 0f);
        _mask.SetParameter("color", new Vector3(0f, 0f, 0f));
        _mask.SetParameter("darknessAlphaOuter", _fade);
        _mask.SetParameter("innerCircleRadius", radius * VignetteInnerRatio);
        _mask.SetParameter("innerCircleMaxRadius", radius * VignetteInnerRatio);
        _mask.SetParameter("outerCircleRadius", radius * VignetteOuterRatio);
        _mask.SetParameter("outerCircleMaxRadius", radius * VignetteOuterRatio);
        handle.UseShader(_mask);
        handle.DrawRect(box, Color.White);

        handle.UseShader(null);
    }

    private static float Smooth(float current, float target, float speed, float delta)
    {
        return target + (current - target) * MathF.Exp(-speed * delta);
    }

    private void ResetState()
    {
        _fade = 0f;
        _smoothedRadius = 0f;
        _smoothedSeverity = 0f;
        _wasActive = false;
        _justActivated = false;
    }

    private float GetCriticalSeverity(EntityUid player)
    {
        var threshold = _entMan.System<MobThresholdSystem>();

        if (!_entMan.TryGetComponent(player, out DamageableComponent? damageable) ||
            !_entMan.TryGetComponent(player, out MobThresholdsComponent? thresholds) ||
            !threshold.TryGetIncapThreshold(player, out var incap, thresholds) ||
            !threshold.TryGetDeadThreshold(player, out var dead, thresholds))
            return 0f;

        var critDamage = incap.Value.Float();
        var deadDamage = dead.Value.Float();
        if (deadDamage <= critDamage)
            return 0f;

        var damage = threshold.CheckVitalDamage(player, damageable).Float();
        return Math.Clamp((damage - critDamage) / (deadDamage - critDamage), 0f, 1f);
    }

    private float GetPreCriticalRadius(EntityUid player, float distance)
    {
        var threshold = _entMan.System<MobThresholdSystem>();

        if (!_entMan.TryGetComponent(player, out DamageableComponent? damageable) ||
            !_entMan.TryGetComponent(player, out MobThresholdsComponent? thresholds) ||
            !threshold.TryGetIncapThreshold(player, out var incap, thresholds))
            return distance * PreCriticalRadiusMax;

        var damage = threshold.CheckVitalDamage(player, damageable);
        var fraction = Math.Clamp(damage.Float() / incap.Value.Float(), 0f, 1f);
        var severity = Math.Clamp((fraction - PreCriticalSeverityStart) / PreCriticalSeverityRange, 0f, 1f);
        return distance * (PreCriticalRadiusMax + (PreCriticalRadiusMin - PreCriticalRadiusMax) * severity);
    }

    private bool TryGetState(out bool dead, out bool critical, out bool preCritical)
    {
        dead = false;
        critical = false;
        preCritical = false;

        var player = _player.LocalEntity;
        if (player == null || !_entMan.TryGetComponent(player.Value, out MobStateComponent? mob))
            return false;

        dead = mob.CurrentState == MobState.Dead;
        critical = mob.CurrentState == MobState.Critical;
        preCritical = mob.CurrentState == MobState.Alive &&
                      _entMan.HasComponent<PreCriticalActiveComponent>(player.Value);

        return dead || critical || preCritical;
    }
}
