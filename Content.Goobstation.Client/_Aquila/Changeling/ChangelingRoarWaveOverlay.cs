using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client._Aquila.Changeling;

public sealed class ChangelingRoarWaveOverlay : Overlay
{
    private const int MaximumInstances = 10;

    private static readonly ProtoId<ShaderPrototype> WavePrototype = "ChangelingRoarWave";

    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private readonly List<WaveInstance> _waves = new();
    private SharedTransformSystem? _transform;
    private int _count;

    private readonly Vector2[] _positions = new Vector2[MaximumInstances];
    private readonly float[] _waveStrengths = new float[MaximumInstances];
    private readonly float[] _waveSpeeds = new float[MaximumInstances];
    private readonly float[] _downScales = new float[MaximumInstances];
    private readonly float[] _fades = new float[MaximumInstances];
    private readonly float[] _times = new float[MaximumInstances];

    public ChangelingRoarWaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(WavePrototype).InstanceUnique();
    }

    /// <summary>
    /// Добавляет волну, привязанную к сущности эффекта.
    /// </summary>
    public void AddWave(EntityUid uid, float waveSpeed, float waveStrength, float downScale, float fadeTime)
    {
        _waves.Add(new WaveInstance(uid, waveSpeed, waveStrength, downScale, fadeTime, _timing.CurTime));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return false;

        if (_transform == null && !_entMan.TrySystem(out _transform))
            return false;

        _count = 0;
        _waves.RemoveAll(wave => (float) (_timing.CurTime - wave.Start).TotalSeconds > wave.FadeTime);

        foreach (var wave in _waves)
        {
            if (!_entMan.TryGetComponent(wave.Entity, out TransformComponent? xform) || xform.MapID != args.MapId)
                continue;

            var local = args.Viewport.WorldToLocal(_transform.GetWorldPosition(xform));
            local.Y = 1 - local.Y / args.Viewport.Size.Y;
            local.X /= args.Viewport.Size.X;

            var time = (float) (_timing.CurTime - wave.Start).TotalSeconds;

            _positions[_count] = local;
            _waveStrengths[_count] = wave.WaveStrength;
            _waveSpeeds[_count] = wave.WaveSpeed;
            _downScales[_count] = wave.DownScale;
            _fades[_count] = 1f - time / wave.FadeTime;
            _times[_count] = time;

            _count++;
            if (_count == MaximumInstances)
                break;
        }

        return _count != 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || args.Viewport.Eye == null)
            return;

        _shader.SetParameter("positions", _positions);
        _shader.SetParameter("waveSpeeds", _waveSpeeds);
        _shader.SetParameter("downScales", _downScales);
        _shader.SetParameter("waveStrengths", _waveStrengths);
        _shader.SetParameter("fades", _fades);
        _shader.SetParameter("times", _times);
        _shader.SetParameter("count", _count);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("renderScale", args.Viewport.RenderScale * args.Viewport.Eye.Scale);

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    private readonly record struct WaveInstance(
        EntityUid Entity,
        float WaveSpeed,
        float WaveStrength,
        float DownScale,
        float FadeTime,
        TimeSpan Start);
}
