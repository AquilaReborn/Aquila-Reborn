using System.Numerics;
using Content.Shared._Aquila.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.Light;

/// <summary>
/// Рисует свечение светильников аддитивно и с учётом карты освещения, так что оно видно только там, куда доходит свет.
/// </summary>
public sealed class LightGlowOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "AquilaLightGlow";

    /// <summary>
    /// Запас вокруг экрана, чтобы свечение ламп за краем не обрезалось.
    /// </summary>
    private const float Margin = 5f;

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly EntityLookupSystem _lookup;
    private readonly ShaderInstance _shader;
    private readonly HashSet<Entity<LightGlowComponent>> _glows = new();
    private readonly Dictionary<SpriteSpecifier, Texture> _textures = new();

    /// <summary>
    /// Множитель яркости свечения из настроек, от 0 до 1.
    /// </summary>
    public float Intensity = 1f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public LightGlowOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entity.System<SharedTransformSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _lookup = _entity.System<EntityLookupSystem>();
        _shader = _prototype.Index(Shader).Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Intensity <= 0f)
            return;

        _glows.Clear();
        _lookup.GetEntitiesIntersecting(args.MapId, args.WorldAABB.Enlarged(Margin), _glows, LookupFlags.Uncontained | LookupFlags.Approximate);
        if (_glows.Count == 0)
            return;

        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        foreach (var (uid, glow) in _glows)
        {
            if (!_entity.TryGetComponent(uid, out PointLightComponent? light) || !light.Enabled)
                continue;

            var alpha = Math.Clamp(light.Energy, 0f, 1f) * glow.Opacity * Intensity;
            if (alpha <= 0f)
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(uid);
            var color = light.Color;
            var center = position + rotation.RotateVec(new Vector2(0f, glow.Offset));
            var box = Box2.CenteredAround(center, new Vector2(glow.Size, glow.Size));
            handle.DrawTextureRect(GetTexture(glow.Texture), new Box2Rotated(box, rotation, center), color.WithAlpha(alpha));
        }

        handle.UseShader(null);
    }

    private Texture GetTexture(SpriteSpecifier specifier)
    {
        if (!_textures.TryGetValue(specifier, out var texture))
        {
            texture = _sprite.Frame0(specifier);
            _textures[specifier] = texture;
        }

        return texture;
    }
}
