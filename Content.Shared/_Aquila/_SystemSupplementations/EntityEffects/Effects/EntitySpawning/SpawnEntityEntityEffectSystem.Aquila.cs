using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

public sealed partial class SpawnEntityEntityEffectSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

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
}
