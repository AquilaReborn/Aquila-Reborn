using System.Numerics;
using Content.Shared._Aquila.PreCritical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
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

    private const float PreCriticalRadius = 0.26f;
    private const float CriticalRadius = 0.14f;
    private const float DeadRadius = 0.08f;

    private static readonly ProtoId<ShaderPrototype> MaskShader = "GradientCircleMask";
    private static readonly ProtoId<ShaderPrototype> RedShader = "GradientCircleMask";
    private static readonly ProtoId<ShaderPrototype> GrayscaleShader = "GreyscaleFullscreen";

    private readonly ShaderInstance _mask;
    private readonly ShaderInstance _red;
    private readonly ShaderInstance _grayscale;

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
        return TryGetState(out _, out _, out _);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!TryGetState(out var dead, out var critical, out var preCritical))
            return;

        var handle = args.WorldHandle;
        var box = args.WorldBounds;
        var view = args.ViewportBounds;
        var distance = MathF.Min(view.Width, view.Height);
        var radius = distance * (dead ? DeadRadius : critical ? CriticalRadius : PreCriticalRadius);
        var beat = 0.5f + 0.5f * MathF.Sin((float) _timing.RealTime.TotalSeconds * 3f);
        var maskRadius = radius * (dead ? 1f : 1f + 0.12f * (beat - 0.5f));

        if (dead && ScreenTexture != null)
        {
            _grayscale.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            handle.UseShader(_grayscale);
            handle.DrawRect(box, Color.White);
        }

        if (critical || preCritical)
        {
            var redInner = maskRadius * (0.55f + 0.15f * beat);
            var redOuter = maskRadius * (1.15f + 0.35f * beat);
            _red.SetParameter("time", 0f);
            _red.SetParameter("color", new Vector3(1f, 0f, 0f));
            _red.SetParameter("darknessAlphaOuter", (critical ? 0.22f : 0.12f) * (0.35f + 0.65f * beat));
            _red.SetParameter("innerCircleRadius", redInner);
            _red.SetParameter("innerCircleMaxRadius", redInner);
            _red.SetParameter("outerCircleRadius", redOuter);
            _red.SetParameter("outerCircleMaxRadius", redOuter);
            handle.UseShader(_red);
            handle.DrawRect(box, Color.White);
        }

        _mask.SetParameter("time", 0f);
        _mask.SetParameter("color", new Vector3(0f, 0f, 0f));
        _mask.SetParameter("darknessAlphaOuter", 1f);
        _mask.SetParameter("innerCircleRadius", maskRadius * 0.3f);
        _mask.SetParameter("innerCircleMaxRadius", maskRadius * 0.3f);
        _mask.SetParameter("outerCircleRadius", maskRadius * 1.05f);
        _mask.SetParameter("outerCircleMaxRadius", maskRadius * 1.05f);
        handle.UseShader(_mask);
        handle.DrawRect(box, Color.White);

        handle.UseShader(null);
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
