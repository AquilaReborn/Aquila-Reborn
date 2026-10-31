using System.Numerics;
using Content.Shared.Aquila.Vehicles;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;

namespace Content.Client.Aquila.Vehicles;

public sealed class VehicleGallopOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _player;
    private readonly IResourceCache _resourceCache;
    private readonly TransformSystem _transform;

    private readonly Dictionary<(ResPath, string, int), Texture?[]> _cache = new();

    public VehicleGallopOverlay(
        IEntityManager entManager,
        IPlayerManager player,
        IResourceCache resourceCache)
    {
        _entManager = entManager;
        _player = player;
        _resourceCache = resourceCache;
        _transform = _entManager.System<TransformSystem>();
    }

    private Texture?[] GetFrames(ResPath path, string prefix, int count)
    {
        count = Math.Max(1, count);
        var key = (path, prefix, count);

        if (_cache.TryGetValue(key, out var frames))
            return frames;

        frames = new Texture?[count];

        if (_resourceCache.TryGetResource<RSIResource>(path, out var resource))
        {
            for (var i = 0; i < count; i++)
            {
                if (resource.RSI.TryGetState($"{prefix}{i}", out var state))
                    frames[i] = state.GetFrame(RsiDirection.South, 0);
            }
        }

        _cache[key] = frames;
        return frames;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var local = _player.LocalEntity;
        if (local == null)
            return;

        if (!_entManager.TryGetComponent(local, out BuckleComponent? buckle)
            || buckle.BuckledTo is not { } horse)
            return;

        if (!_entManager.TryGetComponent(horse, out VehicleGallopComponent? gallop)
            || gallop.Rider != local)
            return;

        if (!_entManager.TryGetComponent(horse, out TransformComponent? xform)
            || xform.MapID != args.MapId)
            return;

        var handle = args.WorldHandle;

        var eyeRot = args.Viewport.Eye?.Rotation ?? default;
        var rotationMatrix = Matrix3Helpers.CreateRotation(-eyeRot);
        var worldPos = _transform.GetWorldPosition(xform);
        var translationMatrix = Matrix3Helpers.CreateTranslation(worldPos);

        handle.SetTransform(Matrix3x2.Multiply(rotationMatrix, translationMatrix));

        var streakFrames = GetFrames(gallop.StreakGaugeRsi, gallop.StreakGaugeStatePrefix, gallop.MaxStreak + 1);
        var streak = Math.Clamp(gallop.Streak, 0, streakFrames.Length - 1);
        DrawGauge(handle, streakFrames[streak], gallop.GaugeHorizontalOffset, gallop);

        if (_entManager.TryGetComponent(horse, out StaminaComponent? stamina))
        {
            var staminaFrames = GetFrames(gallop.StaminaGaugeRsi, gallop.StaminaGaugeStatePrefix, gallop.StaminaDivisions + 1);
            var frame = Math.Clamp(GetStaminaFrame(stamina, gallop.StaminaDivisions), 0, staminaFrames.Length - 1);
            DrawGauge(handle, staminaFrames[frame], gallop.GaugeHorizontalOffset - gallop.StaminaGaugeSpacing, gallop);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    private static void DrawGauge(DrawingHandleWorld handle, Texture? texture, float horizontalOffset, VehicleGallopComponent gallop)
    {
        if (texture == null)
            return;

        var offset = new Vector2(horizontalOffset, -gallop.GaugeVerticalOffset);

        var size = (Vector2) texture.Size /
                   EyeManager.PixelsPerMeter *
                   gallop.GaugeScreenScale;

        handle.DrawTexture(texture, offset - size / 2f);
    }

    private static int GetStaminaFrame(StaminaComponent stamina, int divisions)
    {
        if (stamina.CritThreshold <= 0f)
            return divisions;

        var remaining = 1f - stamina.StaminaDamage / stamina.CritThreshold;
        return (int) MathF.Ceiling(remaining * divisions);
    }
}
