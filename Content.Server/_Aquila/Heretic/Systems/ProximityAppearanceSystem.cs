using Content.Server._Aquila.Heretic.Events;
using Content.Server.Heretic.Components;
using Content.Shared.Ghost;
using Robust.Shared.Player;

namespace Content.Server._Aquila.Heretic.Systems;

/// <summary>
/// Сообщает игрокам о появлении сущности в радиусе видимости.
/// </summary>
public sealed partial class ProximityAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Радиус в тайлах, в котором появление сущности рядом с игроком считается заметным, примерно экран.
    /// Это зона самого рейзера, а не свойство эффекта, поэтому она задаётся здесь, а не в компоненте.
    /// </summary>
    private const float AppearanceRadius = 15f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EldritchInfluenceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<EldritchInfluenceComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Spent)
            return;

        var origin = Transform(ent.Owner);

        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var player, out _, out var xform))
        {
            if (HasComp<GhostComponent>(player))
                continue;

            if (!_transform.InRange((ent.Owner, origin), (player, xform), AppearanceRadius))
                continue;

            var ev = new EntityAppearedNearEvent(ent.Owner, player);
            RaiseLocalEvent(player, ref ev, true);
        }
    }
}
