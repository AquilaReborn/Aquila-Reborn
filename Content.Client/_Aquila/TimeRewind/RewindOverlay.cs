using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using System.Numerics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.Aquila.TimeRewind;

public sealed class RewindOverlay : Overlay
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public RewindOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        var box = args.ViewportBounds;

        var t = (float) _timing.RealTime.TotalSeconds;
        var alpha = 0.18f + 0.07f * MathF.Sin(t * 12f);
        handle.DrawRect(box, new Color(1f, 0f, 0f, alpha));
    }
}
