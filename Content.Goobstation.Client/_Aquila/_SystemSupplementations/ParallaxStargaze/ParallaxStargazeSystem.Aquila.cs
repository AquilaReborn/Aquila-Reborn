using System.Numerics;
using Content.Shared.Projectiles;
using Robust.Shared.Map;

namespace Content.Goobstation.Client.ParallaxStargaze;

public sealed partial class ParallaxStargazeSystem
{
    private void ExemptNearbyProjectiles(MapId mapId, Vector2 firePos, float radiusSq)
    {
        var projectileQuery = EntityQueryEnumerator<ProjectileComponent, TransformComponent>();
        while (projectileQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            if ((_transform.GetWorldPosition(uid) - firePos).LengthSquared() <= radiusSq)
                Exempt.Add(uid);
        }
    }
}
