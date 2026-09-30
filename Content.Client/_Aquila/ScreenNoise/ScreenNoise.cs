using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.ScreenNoise;

public sealed class ScreenNoise : Control
{
    private static readonly ProtoId<ShaderPrototype> Shader = "ScreenNoise";

    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ShaderInstance _shader;

    public float NoiseStrength { get; set; } = 0.002f;
    public float BandStrength { get; set; } = 0.06f;

    public ScreenNoise()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index(Shader).InstanceUnique();
        MouseFilter = MouseFilterMode.Ignore;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        _shader.SetParameter("noiseStrength", NoiseStrength);
        _shader.SetParameter("bandStrength", BandStrength);

        handle.UseShader(_shader);
        handle.DrawRect(PixelSizeBox, Color.White);
        handle.UseShader(null);
    }
}
