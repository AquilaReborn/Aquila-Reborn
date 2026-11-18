// Работа выполнена Claude (Anthropic).

using System.Numerics;
using Content.Client.Graphics;
using Content.Shared._Aquila.EventContent.Atlas739;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.EventContent.Atlas739;

/// <summary>
///     Внешний вид проклятия SoT: персонаж становится полупрозрачным с цветным оттенком (SotCurse.Color).
///     Используется постшейдер (SotCurse): движок сначала рисует ВЕСЬ спрайт (тело, одежду, предметы в руках,
///     волосы) в промежуточный буфер, а шейдер выводит готовый силуэт целиком. Поэтому прозрачность применяется
///     один раз ко всему персонажу, и тело не просвечивает сквозь одежду. Свой экземпляр шейдера у каждого
///     проклятого, чтобы у разных людей мог быть разный цвет.
/// </summary>
public sealed class SotCurseVisualsSystem : EntitySystem
{
    private const string PostShaderId = "sot-curse";
    private static readonly ProtoId<ShaderPrototype> ShaderProto = "SotCurse";

    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly Dictionary<EntityUid, ShaderInstance> _shaders = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SotCurseComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SotCurseComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<SotCurseComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<SotCurseComponent> ent, ref ComponentStartup args)
    {
        Apply(ent);
    }

    private void OnState(Entity<SotCurseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // Цвет могли поменять на лету (VV, повторное наложение): обновляем параметр шейдера.
        Apply(ent);
    }

    private void OnShutdown(Entity<SotCurseComponent> ent, ref ComponentShutdown args)
    {
        if (!_shaders.Remove(ent.Owner, out var shader))
            return;

        if (!TerminatingOrDeleted(ent))
            _sprite.RemovePostShader(ent.Owner, PostShaderId);

        shader.Dispose();
    }

    private void Apply(Entity<SotCurseComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_shaders.TryGetValue(ent.Owner, out var shader))
        {
            shader = _proto.Index(ShaderProto).InstanceUnique();
            _shaders[ent.Owner] = shader;

            _sprite.SetPostShader((ent.Owner, sprite), new SpriteComponent.PostShaderArgs(PostShaderId, shader)
            {
                // Контуры взаимодействия рисуются поверх проклятия, а не под ним.
                Before = ContentPostShaderIds.BeforeOutlines,
            });
        }

        var color = ent.Comp.Color;
        shader.SetParameter("tint", new Vector4(color.R, color.G, color.B, color.A));
    }
}
