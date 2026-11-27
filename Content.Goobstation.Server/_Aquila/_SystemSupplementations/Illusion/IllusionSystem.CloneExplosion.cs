using Content.Goobstation.Shared.Illusion;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage.Systems;

namespace Content.Goobstation.Server.Illusion;

public sealed partial class IllusionSystem
{
    [Dependency] private ExplosionSystem _explosion = default!;

    private void InitializeAquila()
    {
        InitializeSpawnBudgets();
        SubscribeLocalEvent<IllusionComponent, DamageChangedEvent>(OnIllusionDamaged);
    }

    private void OnIllusionDamaged(Entity<IllusionComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        ExplodeIllusion(ent, args.Origin);
    }

    private void ExplodeIllusion(Entity<IllusionComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.Exploded || ent.Comp.ExplosionType is not { } explosionType)
            return;

        ent.Comp.Exploded = true;

        var radius = ent.Comp.ExplosionRadius;
        var intensity = _explosion.RadiusToIntensity(radius, slope: 1f);

        _explosion.QueueExplosion(ent.Owner,
            explosionType,
            intensity,
            slope: 1f,
            maxTileIntensity: radius,
            maxTileBreak: 0,
            canCreateVacuum: false,
            user: user);

        QueueDel(ent.Owner);
    }
}
