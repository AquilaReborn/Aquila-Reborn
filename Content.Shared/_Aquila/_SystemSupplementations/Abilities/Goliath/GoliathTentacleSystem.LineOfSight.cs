using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Map;

namespace Content.Shared.Abilities.Goliath;

public sealed partial class GoliathTentacleSystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    private bool CanSpawnTentacle(EntityUid performer, EntityCoordinates pos)
    {
        return _interaction.InRangeUnobstructed(performer, pos, range: 0f, collisionMask: CollisionGroup.Impassable);
    }
}
