using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using StatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Лунный парад: накладывает безумие на всех, кого пробил снаряд.
/// </summary>
public sealed class MoonParadeSystem : EntitySystem
{
    [Dependency] private readonly MoonSanitySystem _sanity = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private static readonly EntProtoId ConfusionStatus = "StatusEffectWoozy";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoonParadeProjectileComponent, ProjectileHitEvent>(OnHit);
        SubscribeLocalEvent<MoonParadeProjectileComponent, AfterProjectileHitEvent>(OnAfterHit);
    }

    private void OnHit(Entity<MoonParadeProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        var target = args.Target;

        if (args.Shooter == target || !HasComp<MobStateComponent>(target) || _heretic.IsHereticOrGhoul(target))
            return;

        _sanity.ApplyBrainDamage(target, ent.Comp.BrainDamage);
        SpawnAtPosition(ent.Comp.Effect, Transform(target).Coordinates);

        _status.TryAddStatusEffectDuration(target, MoonSanitySystem.InsanityStatus, ent.Comp.HallucinationDuration);
        _status.TryAddStatusEffectDuration(target, ConfusionStatus, ent.Comp.ConfusionDuration);
    }

    private void OnAfterHit(Entity<MoonParadeProjectileComponent> ent, ref AfterProjectileHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target) || !TryComp<ProjectileComponent>(ent, out var projectile))
            return;

        var netTarget = GetNetEntity(args.Target);
        if (!projectile.IgnoredEntities.Contains(netTarget))
        {
            projectile.IgnoredEntities.Add(netTarget);
            Dirty(ent.Owner, projectile);
        }

        projectile.ProjectileSpent = false;
    }
}
