using Content.Server._Aquila.Heretic.Components;
using Content.Server._Aquila.Heretic.Events;
using Content.Server.Heretic.Components;
using Content.Shared.Ghost;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Heretic.Systems;

/// <summary>
/// Сообщает игрокам о появлении сущности в радиусе видимости.
/// </summary>
public sealed partial class ProximityAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Радиус в тайлах, в котором появление сущности рядом с игроком считается заметным, примерно экран.
    /// Это зона самого рейзера, а не свойство эффекта, поэтому она задаётся здесь, а не в компоненте.
    /// </summary>
    private const float AppearanceRadius = 15f;

    /// <summary>
    /// Через сколько сущность снова проверяет, кто находится рядом: появление ловится входом в радиус,
    /// поэтому такая задержка незаметна, а игроки не перебираются каждый тик.
    /// </summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(0.25);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EldritchInfluenceComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<ProximityAppearanceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var appearance, out var xform))
        {
            if (appearance.NextCheck > now)
                continue;

            appearance.NextCheck = now + CheckInterval;

            NotifyNearby(uid, xform, appearance);
        }
    }

    private void OnMapInit(Entity<EldritchInfluenceComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Spent)
            return;

        EnsureComp<ProximityAppearanceComponent>(ent);
    }

    private void NotifyNearby(EntityUid uid, TransformComponent xform, ProximityAppearanceComponent appearance)
    {
        var notified = appearance.Notified;

        notified.RemoveWhere(player => !HasComp<ActorComponent>(player)
            || !TryComp(player, out TransformComponent? playerXform)
            || !_transform.InRange((uid, xform), (player, playerXform), AppearanceRadius));

        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var player, out _, out var playerXform))
        {
            if (notified.Contains(player))
                continue;

            if (HasComp<GhostComponent>(player))
                continue;

            if (!_transform.InRange((uid, xform), (player, playerXform), AppearanceRadius))
                continue;

            notified.Add(player);

            var ev = new EntityAppearedNearEvent(uid, player);
            RaiseLocalEvent(player, ref ev, true);
        }
    }
}
