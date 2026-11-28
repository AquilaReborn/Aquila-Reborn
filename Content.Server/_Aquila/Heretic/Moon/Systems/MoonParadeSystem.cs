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

    /// <summary>
    /// Подключает попадания и сквозное движение Лунного парада.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoonParadeProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<MoonParadeProjectileComponent, AfterProjectileHitEvent>(OnAfterProjectileHit);
    }

    private void OnProjectileHit(Entity<MoonParadeProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        TryApplyParadeHit((ent.Owner, ent.Comp), args.Target, args.Shooter);
    }

    /// <summary>
    /// Накладывает эффекты Лунного парада на допустимую жертву попадания.
    /// </summary>
    public bool TryApplyParadeHit(Entity<MoonParadeProjectileComponent?> ent, EntityUid target, EntityUid? user)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!CanApplyParadeHit(target, user))
            return false;

        DoApplyParadeHit((ent.Owner, ent.Comp), target);
        return true;
    }

    private bool CanApplyParadeHit(EntityUid target, EntityUid? user, bool quiet = false)
    {
        return user != target && !TerminatingOrDeleted(target) && HasComp<MobStateComponent>(target) &&
            !_heretic.IsHereticOrGhoul(target);
    }

    private void DoApplyParadeHit(Entity<MoonParadeProjectileComponent> ent, EntityUid target)
    {
        _sanity.ApplyBrainDamage(target, ent.Comp.BrainDamage);
        SpawnAtPosition(ent.Comp.Effect, Transform(target).Coordinates);

        _status.TryAddStatusEffectDuration(target, MoonSanitySystem.InsanityStatus, ent.Comp.HallucinationDuration);
        _status.TryAddStatusEffectDuration(target, ConfusionStatus, ent.Comp.ConfusionDuration);
        _status.TryUpdateStatusEffectDuration(target, ent.Comp.FractureStatus, ent.Comp.FractureDuration);
    }

    private void OnAfterProjectileHit(Entity<MoonParadeProjectileComponent> ent, ref AfterProjectileHitEvent args)
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
