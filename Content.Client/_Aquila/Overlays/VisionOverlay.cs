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

    private static readonly ProtoId<ShaderPrototype> MaskShader = "GradientCircleMask";
    private static readonly ProtoId<ShaderPrototype> RedShader = "GradientCircleMask";
    private static readonly ProtoId<ShaderPrototype> GrayscaleShader = "GreyscaleFullscreen";

    private readonly ShaderInstance _mask;
    private readonly ShaderInstance _red;
    private readonly ShaderInstance _grayscale;

    private float _smoothedRadius;
    private float _smoothedSeverity;
    private float _fade;
    private bool _wasActive;
    private bool _justActivated;
    private TimeSpan _lastUpdate;

    public VisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _mask = _proto.Index(MaskShader).InstanceUnique();
        _red = _proto.Index(RedShader).InstanceUnique();
        _grayscale = _proto.Index(GrayscaleShader).InstanceUnique();
        ZIndex = 50;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!TryGetState(out _, out _, out _))
        {
            _wasActive = false;
            _smoothedRadius = 0f;
            _fade = 0f;
            return false;
        }

        if (!_wasActive)
        {
            _wasActive = true;
            _justActivated = true;
            _fade = 0f;
            _lastUpdate = _timing.RealTime;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!TryGetState(out var dead, out var critical, out var preCritical))
            return;

        var player = _player.LocalEntity;
        if (player == null)
            return;

        var handle = args.WorldHandle;
        var box = args.WorldBounds;
        var view = args.ViewportBounds;
        var distance = MathF.Min(view.Width, view.Height);

        var delta = Math.Clamp((float) (_timing.RealTime - _lastUpdate).TotalSeconds, 0f, 0.1f);
        _lastUpdate = _timing.RealTime;
        _fade = Smooth(_fade, 1f, FadeSpeed, delta);

        if (critical)
        {
            var severity = GetCriticalSeverity(player.Value);

            if (_justActivated)
            {
                _smoothedSeverity = severity;
            }
            else
            {
                _smoothedSeverity = Smooth(_smoothedSeverity, severity, RadiusSmoothSpeed, delta);
            }

            var critBeat = 0.5f + 0.5f * MathF.Sin((float) _timing.RealTime.TotalSeconds * 3f);
            var criticalTargetRadius = distance * (0.15f + (0.08f - 0.15f) * severity) * (1f + 0.06f * (critBeat - 0.5f));

            if (_justActivated)
            {
                _smoothedRadius = criticalTargetRadius;
                _justActivated = false;
            }
            else
            {
                _smoothedRadius = Smooth(_smoothedRadius, criticalTargetRadius, RadiusSmoothSpeed, delta);
            }

            var maskRadius = _smoothedRadius;

            _mask.SetParameter("time", 0f);
            _mask.SetParameter("color", Vector3.One);
            _mask.SetParameter("darknessAlphaOuter", 0.35f * (0.4f + 0.6f * critBeat) * _fade);
            _mask.SetParameter("innerCircleRadius", 0f);
            _mask.SetParameter("innerCircleMaxRadius", 0f);
            _mask.SetParameter("outerCircleRadius", maskRadius);
            _mask.SetParameter("outerCircleMaxRadius", maskRadius);
            handle.UseShader(_mask);
            handle.DrawRect(box, Color.White);

            _mask.SetParameter("time", 0f);
            _mask.SetParameter("color", new Vector3(0f, 0f, 0f));
            _mask.SetParameter("darknessAlphaOuter", _fade);
            _mask.SetParameter("innerCircleRadius", maskRadius * 0.3f);
            _mask.SetParameter("innerCircleMaxRadius", maskRadius * 0.3f);
            _mask.SetParameter("outerCircleRadius", maskRadius * 1.05f);
            _mask.SetParameter("outerCircleMaxRadius", maskRadius * 1.05f);
            handle.UseShader(_mask);
            handle.DrawRect(box, Color.White);
            handle.UseShader(null);

            var hazeInner = distance * (0.6f - 0.58f * _smoothedSeverity);
            var hazeOuter = distance * (2f - _smoothedSeverity);

            _mask.SetParameter("time", critBeat);
            _mask.SetParameter("color", Vector3.One);
            _mask.SetParameter("darknessAlphaOuter", 0.6f * _fade);
            _mask.SetParameter("innerCircleRadius", hazeInner);
            _mask.SetParameter("innerCircleMaxRadius", hazeInner + 0.005f * distance);
            _mask.SetParameter("outerCircleRadius", hazeOuter);
            _mask.SetParameter("outerCircleMaxRadius", hazeOuter + 0.2f * distance);
            handle.UseShader(_mask);
            handle.DrawRect(box, Color.White);
            handle.UseShader(null);
            return;
        }

        var targetRadius = dead ? distance * DeadRadius : GetPreCriticalRadius(player.Value, distance);

        if (_justActivated)
        {
            _smoothedRadius = targetRadius;
            _justActivated = false;
        }
        else
        {
            _smoothedRadius = Smooth(_smoothedRadius, targetRadius, RadiusSmoothSpeed, delta);
        }

        var radius = _smoothedRadius;
        var beat = 0.5f + 0.5f * MathF.Sin((float) _timing.RealTime.TotalSeconds * 3f);

        if (dead && ScreenTexture != null)
        {
            _grayscale.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            handle.UseShader(_grayscale);
            handle.DrawRect(box, Color.White);
        }

        if (preCritical)
        {
            var maskRadius = radius * (1f + 0.12f * (beat - 0.5f));
            var redInner = maskRadius * (0.55f + 0.15f * beat);
            var redOuter = maskRadius * (1.15f + 0.35f * beat);
            _red.SetParameter("time", 0f);
            _red.SetParameter("color", new Vector3(1f, 0f, 0f));
            _red.SetParameter("darknessAlphaOuter", 0.12f * (0.35f + 0.65f * beat) * _fade);
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
        _mask.SetParameter("innerCircleRadius", radius * 0.3f);
        _mask.SetParameter("innerCircleMaxRadius", radius * 0.3f);
        _mask.SetParameter("outerCircleRadius", radius * 1.05f);
        _mask.SetParameter("outerCircleMaxRadius", radius * 1.05f);
        handle.UseShader(_mask);
        handle.DrawRect(box, Color.White);

        handle.UseShader(null);
    }

    private static float Smooth(float current, float target, float speed, float delta)
    {
        return target + (current - target) * MathF.Exp(-speed * delta);
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
        var severity = Math.Clamp((fraction - 0.75f) / 0.25f, 0f, 1f);
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
