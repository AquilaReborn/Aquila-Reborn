using Content.Server._Aquila.Cyberware.Components;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared._Aquila.Cyberware.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server._Aquila.Cyberware;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SuppressCyberpsychosisEntityEffectSystem : EntityEffectSystem<CyberpsychosisComponent, SuppressCyberpsychosis>
{
    [Dependency] private readonly CyberpsychosisSystem _cyberpsychosis = default!;

    protected override void Effect(Entity<CyberpsychosisComponent> entity, ref EntityEffectEvent<SuppressCyberpsychosis> args)
    {
        _cyberpsychosis.Suppress(entity.AsNullable(), args.Effect.Time * args.Scale);
    }
}

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CyberpsychosisTherapyEntityEffectSystem : EntityEffectSystem<CyberpsychosisComponent, CyberpsychosisTherapy>
{
    [Dependency] private readonly CyberpsychosisSystem _cyberpsychosis = default!;

    protected override void Effect(Entity<CyberpsychosisComponent> entity, ref EntityEffectEvent<CyberpsychosisTherapy> args)
    {
        _cyberpsychosis.ApplyTherapy(entity.AsNullable(), args.Effect.Amount * args.Scale);
    }
}

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CyberpsychosisResistanceEntityEffectSystem : EntityEffectSystem<CyberLoadComponent, CyberpsychosisResistance>
{
    [Dependency] private readonly CyberpsychosisResistanceSystem _resistance = default!;

    protected override void Effect(Entity<CyberLoadComponent> entity, ref EntityEffectEvent<CyberpsychosisResistance> args)
    {
        _resistance.AddResistance(entity, args.Effect.Time * args.Scale, args.Effect.Multiplier);
    }
}
