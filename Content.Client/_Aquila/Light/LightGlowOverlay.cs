using System.Numerics;
using Content.Shared._Aquila.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Light;

/// <summary>
/// Рисует свечение светильников аддитивно и с учётом карты освещения, так что оно видно только там, куда доходит свет.
/// </summary>
public sealed class LightGlowOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "AquilaLightGlow";

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public LightGlowOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entity.System<SharedTransformSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _shader = _prototype.Index(Shader).Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        var query = _entity.EntityQueryEnumerator<LightGlowComponent, PointLightComponent, TransformComponent>();
        while (query.MoveNext(out _, out var glow, out var light, out var xform))
        {
            if (xform.MapID != args.MapId || !light.Enabled)
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform);
            if (!args.WorldAABB.Enlarged(glow.Size).Contains(position))
                continue;

            var energy = light.Energy;
            var alpha = Math.Clamp(energy, 0f, 1f) * glow.Opacity;
            if (alpha <= 0f)
                continue;

            var color = light.Color;
            var center = position + rotation.RotateVec(new Vector2(0f, glow.Offset));
            var box = Box2.CenteredAround(center, new Vector2(glow.Size, glow.Size));
            handle.DrawTextureRect(_sprite.Frame0(glow.Texture), new Box2Rotated(box, rotation, center), color.WithAlpha(alpha));
        }

        handle.UseShader(null);
    }
}
