using Content.Client.Graphics;
using Content.Shared._Aquila.Engram;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Engram;

/// <summary>
/// Рисует энграмму мерцающей голограммой с помехами.
/// </summary>
public sealed class EngramVisualsSystem : EntitySystem
{
    private const string PostShaderId = "engram-hologram";
    private static readonly ProtoId<ShaderPrototype> Shader = "EngramHologram";

    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EngramComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EngramComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<EngramComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetPostShader((ent.Owner, sprite), new SpriteComponent.PostShaderArgs(PostShaderId, _proto.Index(Shader).Instance())
        {
            Before = ContentPostShaderIds.BeforeOutlines,
        });
    }

    private void OnShutdown(Entity<EngramComponent> ent, ref ComponentShutdown args)
    {
        _sprite.RemovePostShader(ent.Owner, PostShaderId);
    }
}
