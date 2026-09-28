using Robust.Shared.Containers; // Aquila Change
using Robust.Shared.Map; // Aquila Change
using Robust.Shared.Network;
using Robust.Shared.Physics.Components; // Aquila Change
using Robust.Shared.Physics.Systems; // Aquila Change
using Robust.Shared.Prototypes; // Aquila Change

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a number of entities of a given prototype at the coordinates of this entity.
/// Amount is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpawnEntityEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntity>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!; // Aquila Change
    [Dependency] private readonly SharedContainerSystem _container = default!; // Aquila Change
    [Dependency] private readonly SharedPhysicsSystem _physics = default!; // Aquila Change

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnEntity> args)
    {
        var quantity = args.Effect.ShouldScale ? args.Effect.Number * (int) Math.Floor(args.Scale) : args.Effect.Number; // Goobstation - Added ShouldSCale
        var proto = args.Effect.Entity;

        if (args.Effect.Predicted)
        {
            for (var i = 0; i < quantity; i++)
            {
                RestoreCollision(PredictedSpawnNextToOrDrop(proto, entity, entity.Comp), proto); // Aquila Change
            }
        }
        else if (_net.IsServer)
        {
            for (var i = 0; i < quantity; i++)
            {
                RestoreCollision(SpawnNextToOrDrop(proto, entity, entity.Comp), proto); // Aquila Change
            }
        }
    }

    // Aquila Change start
    private void RestoreCollision(EntityUid uid, EntProtoId proto)
    {
        if (!TryComp<PhysicsComponent>(uid, out var body)
            || body.CanCollide
            || Transform(uid).MapID == MapId.Nullspace
            || _container.IsEntityOrParentInContainer(uid))
            return;

        if (!_proto.Index(proto).TryGetComponent<PhysicsComponent>(out var protoBody, EntityManager.ComponentFactory)
            || !protoBody.CanCollide)
            return;

        _physics.SetCanCollide(uid, true, body: body);
    }
    // Aquila Change end
}

/// <inheritdoc cref="BaseSpawnEntityEntityEffect{T}"/>
public sealed partial class SpawnEntity : BaseSpawnEntityEntityEffect<SpawnEntity>;
