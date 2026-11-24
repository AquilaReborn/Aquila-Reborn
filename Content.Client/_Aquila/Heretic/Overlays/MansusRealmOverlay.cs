using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Heretic.Overlays;

/// <summary>
/// Фиолетовая дымка, щупальца и прожилки по краям экрана: то, что видит жертва испытания Мансуса
/// и еретик при срабатывании зрения сдвига реальности.
/// </summary>
public sealed class MansusRealmOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly ProtoId<ShaderPrototype> MadnessShader = "MansusMadness";

    private readonly ShaderInstance _shader;

    /// <summary>
    /// Сила эффекта от 0 до 1: общий параметр прозрачности для испытания Мансуса и зрения еретика.
    /// </summary>
    public float Intensity;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public override bool RequestScreenTexture => true;

    public MansusRealmOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(MadnessShader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (Intensity <= 0f
            || !_entity.TryGetComponent(_player.LocalEntity, out EyeComponent? eye)
            || args.Viewport.Eye != eye.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("intensity", Intensity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
