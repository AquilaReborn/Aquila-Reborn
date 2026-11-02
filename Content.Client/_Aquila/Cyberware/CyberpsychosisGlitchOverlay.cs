using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Cyberware;

/// <summary>
/// Глитчи по краям экрана у больного киберпсихозом.
/// </summary>
public sealed class CyberpsychosisGlitchOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "CyberpsychosisGlitch";

    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    /// <summary>
    /// Текущая сила глитча от 0 до 1.
    /// </summary>
    public float Intensity;

    /// <summary>
    /// Насколько экран залит красным, от 0 до 1.
    /// </summary>
    public float Redness;

    public CyberpsychosisGlitchOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(Shader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if ((Intensity <= 0f && Redness <= 0f)
            || !_entMan.TryGetComponent(_player.LocalEntity, out EyeComponent? eye))
            return false;

        return args.Viewport.Eye == eye.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("intensity", Intensity);
        _shader.SetParameter("redness", Redness);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
