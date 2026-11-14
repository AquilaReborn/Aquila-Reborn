using System.Numerics;
using Content.Shared._Aquila.Heretic;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.Heretic;

/// <summary>
/// Рисует стрелку живого сердца, которая висит рядом с еретиком и указывает на жертву.
/// </summary>
public sealed class HereticHeartWaypointerOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly ResPath MarkerRsi = new("/Textures/_Aquila/Markers/Waypointers/waypointer.rsi");

    private const int MarkerStateCount = 5;
    private const float MarkerSize = 2f;
    private const float MinDirectionLengthSquared = 0.01f;

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly SharedHereticHeartWaypointerSystem _waypointer;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;
    private readonly ShaderInstance _shader;
    private readonly Texture?[] _textures = new Texture?[MarkerStateCount];

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public HereticHeartWaypointerOverlay()
    {
        IoCManager.InjectDependencies(this);

        _waypointer = _entity.System<SharedHereticHeartWaypointerSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<SharedTransformSystem>();
        _shader = _proto.Index(UnshadedShader).Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } user
            || !_waypointer.TryGetWaypointer(user, out _, out var targetPosition, out var distance, out var sameMap, out var maxRange, out var offset)
            || !sameMap)
            return;

        if (!_entity.TryGetComponent(user, out TransformComponent? xform) || xform.MapID != args.MapId)
            return;

        var userPosition = _transform.GetWorldPosition(xform);
        var direction = targetPosition - userPosition;
        if (direction.LengthSquared() < MinDirectionLengthSquared)
            return;

        direction = direction.Normalized();

        var t = maxRange > 0f ? Math.Clamp(distance / maxRange, 0f, 1f) : 1f;
        var index = Math.Clamp(MarkerStateCount - (int) MathF.Floor(t * MarkerStateCount), 1, MarkerStateCount);
        var color = Color.InterpolateBetween(Color.Lime, Color.Red, t);
        var angle = direction.ToWorldAngle() + MathHelper.Pi;

        var texture = GetTexture(index);
        var center = userPosition + direction * offset;
        var box = Box2.CenteredAround(center, new Vector2(MarkerSize, MarkerSize));

        var handle = args.WorldHandle;
        var oldShader = handle.GetShader();
        var oldTransform = handle.GetTransform();

        handle.UseShader(_shader);
        handle.DrawTextureRectRegion(texture, new Box2Rotated(box, angle, center), color);

        handle.UseShader(oldShader);
        handle.SetTransform(oldTransform);
    }

    private Texture GetTexture(int index)
    {
        var cached = _textures[index - 1];
        if (cached != null)
            return cached;

        var texture = _sprite.Frame0(new SpriteSpecifier.Rsi(MarkerRsi, $"marker{index}"));
        _textures[index - 1] = texture;

        return texture;
    }
}
