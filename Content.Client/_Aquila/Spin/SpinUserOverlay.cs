
using System.Numerics;
using Content.Shared.Aquila.Spin;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;

namespace Content.Client.Aquila.Spin;

public sealed class SpinUserOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _player;
    private readonly TransformSystem _transform;

    private readonly Texture[] _frames;

    public SpinUserOverlay(
        IEntityManager entManager,
        IPlayerManager player,
        IResourceCache resourceCache)
    {
        _entManager = entManager;
        _player = player;
        _transform = _entManager.System<TransformSystem>();

        var rsi = resourceCache.GetResource<RSIResource>(
            "/Textures/_Aquila/Other/Spin/spin_gauge.rsi").RSI;

        _frames = new Texture[8];

        for (var i = 0; i < _frames.Length; i++)
        {
            if (rsi.TryGetState($"spin_{i}", out var state))
                _frames[i] = state.GetFrame(RsiDirection.South, 0);
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var local = _player.LocalEntity;
        if (local == null)
            return;

        if (!_entManager.TryGetComponent(local, out SpinUserComponent? spin))
            return;

        if (!_entManager.TryGetComponent(local, out TransformComponent? xform))
            return;

        if (xform.MapID != args.MapId)
            return;

        var charges = Math.Clamp(spin.Charges, 0, _frames.Length - 1);
        var texture = _frames[charges];

        if (texture == null)
            return;

        var handle = args.WorldHandle;

        var eyeRot = args.Viewport.Eye?.Rotation ?? default;
        var rotationMatrix = Matrix3Helpers.CreateRotation(-eyeRot);

        var worldPos = _transform.GetWorldPosition(xform);

        var offset = new Vector2(
            0f,
            -spin.GaugeVerticalOffset);

        var translationMatrix = Matrix3Helpers.CreateTranslation(worldPos);
        var matty = Matrix3x2.Multiply(
            rotationMatrix,
            translationMatrix);

        handle.SetTransform(matty);

        var size = (Vector2) texture.Size /
                   EyeManager.PixelsPerMeter *
                   spin.GaugeScreenScale;

        handle.DrawTexture(texture, offset - size / 2f);

        handle.SetTransform(Matrix3x2.Identity);
    }
}
