using Content.Shared._Aquila.Engram;
using Content.Shared.Camera;

namespace Content.Client._Aquila.Engram;

/// <summary>
/// Энграмма смотрит как ИИ через камеру: картинка центрируется на ней самой,
/// а поле зрения строится от глаз носителя, на которого нацелен её глаз.
/// </summary>
public sealed class EngramEyeSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EngramComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
    }

    private void OnGetEyeOffset(Entity<EngramComponent> ent, ref GetEyeOffsetEvent args)
    {
        if (!TryComp<EyeComponent>(ent, out var eye)
            || eye.Target is not { } host
            || host == ent.Owner
            || !TryComp<TransformComponent>(host, out var hostXform))
            return;

        var xform = Transform(ent);
        if (xform.MapID != hostXform.MapID)
            return;

        args.Offset += _transform.GetWorldPosition(xform) - _transform.GetWorldPosition(hostXform);
    }
}
