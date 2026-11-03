using Content.Shared._Aquila.Cyberware.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;

namespace Content.Server._Aquila.Cyberware;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CyberpsychosisRampageEntityEffectSystem : EntityEffectSystem<HandsComponent, CyberpsychosisRampage>
{
    [Dependency] private readonly CyberpsychosisRampageSystem _rampage = default!;

    protected override void Effect(Entity<HandsComponent> entity, ref EntityEffectEvent<CyberpsychosisRampage> args)
    {
        _rampage.TryStartRampage(entity, args.Effect, args.Scale);
    }
}
