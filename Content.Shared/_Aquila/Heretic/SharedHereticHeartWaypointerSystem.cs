using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Aquila.Heretic;

/// <summary>
/// Ставит и обновляет стрелку живого сердца, которая ведёт еретика к жертве.
/// </summary>
public sealed class SharedHereticHeartWaypointerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Обновляет позицию жертвы для активных стрелок и убирает истёкшие.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<HereticHeartWaypointerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryGetEntity(comp.Target, out var target) || TerminatingOrDeleted(target.Value))
            {
                RemCompDeferred(uid, comp);
                continue;
            }

            var targetXform = Transform(target.Value);
            if (curTime >= comp.ExpiresAt || targetXform.MapID == MapId.Nullspace)
            {
                RemCompDeferred(uid, comp);
                continue;
            }

            if (curTime < comp.NextUpdate)
                continue;

            comp.NextUpdate = curTime + UpdateInterval;

            var userXform = Transform(uid);
            var targetPosition = _transform.GetWorldPosition(targetXform);
            var distance = (targetPosition - _transform.GetWorldPosition(userXform)).Length();
            var sameMap = targetXform.MapID == userXform.MapID;

            if (comp.TargetPosition == targetPosition && comp.Distance == distance && comp.SameMap == sameMap)
                continue;

            comp.TargetPosition = targetPosition;
            comp.Distance = distance;
            comp.SameMap = sameMap;
            Dirty(uid, comp);
        }
    }

    /// <summary>
    /// Наводит стрелку живого сердца на жертву и продлевает время её жизни.
    /// </summary>
    public void SetTarget(EntityUid user, EntityUid target)
    {
        var comp = EnsureComp<HereticHeartWaypointerComponent>(user);
        var userXform = Transform(user);
        var targetXform = Transform(target);
        var userPosition = _transform.GetWorldPosition(userXform);
        var targetPosition = _transform.GetWorldPosition(targetXform);

        comp.Target = GetNetEntity(target);
        comp.TargetPosition = targetPosition;
        comp.Distance = (targetPosition - userPosition).Length();
        comp.SameMap = targetXform.MapID == userXform.MapID;
        comp.NextUpdate = _timing.CurTime;
        comp.ExpiresAt = _timing.CurTime + comp.Duration;

        Dirty(user, comp);
    }

    /// <summary>
    /// Убирает стрелку живого сердца.
    /// </summary>
    public void ClearTarget(EntityUid user)
    {
        RemCompDeferred<HereticHeartWaypointerComponent>(user);
    }

    /// <summary>
    /// Читает данные стрелки живого сердца для указанной сущности.
    /// </summary>
    public bool TryGetWaypointer(EntityUid user,
        out NetEntity? target,
        out Vector2 targetPosition,
        out float distance,
        out bool sameMap,
        out float maxRange,
        out float offset)
    {
        target = null;
        targetPosition = Vector2.Zero;
        distance = 0f;
        sameMap = false;
        maxRange = 0f;
        offset = 0f;

        if (!TryComp<HereticHeartWaypointerComponent>(user, out var comp))
            return false;

        if (_timing.CurTime >= comp.ExpiresAt)
            return false;

        target = comp.Target;
        targetPosition = comp.TargetPosition;
        distance = comp.Distance;
        sameMap = comp.SameMap;
        maxRange = comp.MaxRange;
        offset = comp.Offset;

        return true;
    }
}
