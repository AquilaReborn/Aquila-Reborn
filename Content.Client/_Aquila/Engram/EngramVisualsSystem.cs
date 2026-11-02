using Content.Shared._Aquila.Engram;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Engram;

/// <summary>
/// Рисует энграмму мерцающей голограммой с помехами.
/// Шейдер восстанавливается каждый кадр, потому что подсветка при наведении сбрасывает его.
/// </summary>
public sealed class EngramVisualsSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> Shader = "EngramHologram";

    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EngramComponent, ComponentShutdown>(OnShutdown);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<EngramComponent, SpriteComponent>();
        while (query.MoveNext(out _, out _, out var sprite))
        {
            if (sprite.PostShader == null)
                sprite.PostShader = _proto.Index(Shader).Instance();
        }
    }

    private void OnShutdown(Entity<EngramComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            sprite.PostShader = null;
    }
}
