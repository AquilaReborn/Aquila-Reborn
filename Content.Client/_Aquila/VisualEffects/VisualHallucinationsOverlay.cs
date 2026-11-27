using System.Numerics;
using Content.Shared._Aquila.VisualEffects;
using Content.Shared.StatusEffectNew.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Aquila.VisualEffects;

/// <summary>
/// Рисует отдельные полупрозрачные образы только в основном обзоре их жертвы.
/// </summary>
public sealed class VisualHallucinationsOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    /// <summary>
    /// Подключает зависимости отрисовки.
    /// </summary>
    public VisualHallucinationsOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _entities.TryGetComponent(_player.LocalEntity, out EyeComponent? eye)
            && args.Viewport.Eye == eye.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entities.TryGetComponent(_player.LocalEntity, out VisualHallucinationsComponent? visuals)
            || visuals.Image == null
            || visuals.MapId != args.MapId
            || visuals.EndsAt <= _timing.RealTime
            || !_entities.TryGetComponent(visuals.Source, out VisualHallucinationsStatusEffectComponent? settings)
            || !_entities.TryGetComponent(visuals.Source, out StatusEffectComponent? effect)
            || !effect.Applied
            || effect.AppliedTo != _player.LocalEntity
            || effect.EndEffectTime is { } end && end <= _timing.CurTime)
            return;

        var progress = (float) ((_timing.RealTime - visuals.StartedAt) / (visuals.EndsAt - visuals.StartedAt));
        var opacity = Math.Clamp(settings.Opacity, 0, 1) * MathF.Sin(Math.Clamp(progress, 0, 1) * MathF.PI);
        var texture = GetImageTexture(visuals.Image, _timing.RealTime - visuals.StartedAt);
        if (texture == null)
            return;

        args.WorldHandle.DrawTextureRect(texture,
            Box2.CenteredAround(visuals.Position, new Vector2(settings.ImageSize)), Color.White.WithAlpha(opacity));
    }

    private Texture? GetImageTexture(SpriteSpecifier image, TimeSpan age)
    {
        if (image is SpriteSpecifier.Texture texture)
        {
            return _cache.TryGetResource(SpriteSystem.TextureRoot / texture.TexturePath, out TextureResource? resource)
                ? resource.Texture
                : null;
        }

        if (image is not SpriteSpecifier.Rsi rsi)
            return null;

        var path = SpriteSystem.TextureRoot / rsi.RsiPath;
        if (!_cache.TryGetResource(path, out RSIResource? rsiResource)
            || !rsiResource.RSI.TryGetState(rsi.RsiState, out _))
            return null;

        return _entities.System<SpriteSystem>().GetFrame(new SpriteSpecifier.Rsi(path, rsi.RsiState), age);
    }
}
