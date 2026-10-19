using Content.Shared.Aquila.TimeStop;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Enums;

namespace Content.Client.Aquila.TimeStop;

public sealed class TimeStopOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _grayscaleShader;
    private readonly ShaderInstance _flashShader;

    public TimeStopOverlay()
    {
        IoCManager.InjectDependencies(this);
        _grayscaleShader = _proto.Index<ShaderPrototype>("TimeStopGrayscale").InstanceUnique(); //inv
        _flashShader = _proto.Index<ShaderPrototype>("TimeStopFlash").InstanceUnique();
        ZIndex = 10;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        return player != null && _entMan.HasComponent<TimeStopSlowedComponent>(player.Value);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        if (player == null ||
            !_entMan.TryGetComponent<TimeStopSlowedComponent>(player.Value, out var slowed))
            return;

        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var box = args.WorldBounds;
        var now = _timing.CurTime;

        if (now < slowed.FlashEndTime)
        {
            var progress = slowed.FlashDuration > 0f
                ? 1f - (float) (slowed.FlashEndTime - now).TotalSeconds / slowed.FlashDuration
                : 1f;

            _flashShader.SetParameter("progress", progress);
            handle.UseShader(_flashShader);
            handle.DrawRect(box, Color.White);
            handle.UseShader(null);
            return;
        }

        if (slowed.Grayscale)
        {
            _grayscaleShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            handle.UseShader(_grayscaleShader);
            handle.DrawRect(box, Color.White);
            handle.UseShader(null);
        }
    }
}
