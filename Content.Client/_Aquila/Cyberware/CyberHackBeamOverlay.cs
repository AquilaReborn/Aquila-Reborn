using Content.Shared._Aquila.Cyberware.Hack;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Cyberware;

/// <summary>
/// Рисует зелёное цифровое полотно от взломщика к взламываемому устройству.
/// </summary>
public sealed class CyberHackBeamOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "CyberHackBeam";
    private const float MinLength = 0.05f;

    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly SharedTransformSystem _transform;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CyberHackBeamOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entMan.System<SharedTransformSystem>();
        _shader = _proto.Index(Shader).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var query = _entMan.EntityQueryEnumerator<CyberHackBeamComponent, TransformComponent>();
        while (query.MoveNext(out _, out var beam, out var xform))
        {
            if (beam.Target is not { } target
                || !_entMan.TryGetComponent(target, out TransformComponent? targetXform)
                || xform.MapID != args.MapId
                || targetXform.MapID != args.MapId)
                continue;

            var start = _transform.GetWorldPosition(xform);
            var end = _transform.GetWorldPosition(targetXform);
            var diff = end - start;
            var length = diff.Length();
            if (length < MinLength)
                continue;

            var middle = start + diff / 2f;
            var box = new Box2(-beam.Width / 2f, -length / 2f, beam.Width / 2f, length / 2f).Translated(middle);
            var rotated = new Box2Rotated(box, diff.ToWorldAngle(), middle);

            _shader.SetParameter("beamLength", length);
            handle.UseShader(_shader);
            handle.DrawTextureRect(Texture.White, rotated);
        }

        handle.UseShader(null);
    }
}
