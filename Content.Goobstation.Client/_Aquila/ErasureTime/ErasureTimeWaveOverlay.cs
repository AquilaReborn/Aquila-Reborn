// ErasureTimeWaveOverlay.cs (клиент)
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.Aquila.ErasureTime;

public sealed class ErasureTimeWaveOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly ErasureTimeWaveOverlaySystem _system;
    private readonly ShaderInstance _shader;

    public ErasureTimeWaveOverlay(ErasureTimeWaveOverlaySystem system)
    {
        IoCManager.InjectDependencies(this);
        _system = system;
        _shader = _protoManager.Index<ShaderPrototype>("ErasureTimeWave").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _system.ActiveWave != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var progress = _system.GetProgress();
        if (progress <= 0f)
            return;

        _shader.SetParameter("progress", progress);

        var handle = args.ScreenHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.ViewportBounds, Color.White);
        handle.UseShader(null);
    }
}
