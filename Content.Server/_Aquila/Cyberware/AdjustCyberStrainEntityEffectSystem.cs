using Content.Server._Aquila.Cyberware.Components;
using Content.Shared._Aquila.Cyberware.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server._Aquila.Cyberware;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AdjustCyberStrainEntityEffectSystem : EntityEffectSystem<CyberLoadComponent, AdjustCyberStrain>
{
    [Dependency] private readonly CyberLoadSystem _cyberLoad = default!;

    protected override void Effect(Entity<CyberLoadComponent> entity, ref EntityEffectEvent<AdjustCyberStrain> args)
    {
        _cyberLoad.AdjustStrain(entity.AsNullable(), args.Effect.Amount * args.Scale);
    }
}
