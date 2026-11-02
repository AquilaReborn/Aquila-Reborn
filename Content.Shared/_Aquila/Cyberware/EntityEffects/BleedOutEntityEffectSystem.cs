using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.EntityEffects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class BleedOutEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, BleedOut>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<BleedOut> args)
    {
        if (_net.IsClient)
            return;

        if (!_bloodstream.TryBleedOut(entity.AsNullable(), args.Effect.Amount * args.Scale))
            return;

        _audio.PlayPvs(args.Effect.Sound, entity);
    }
}

/// <summary>
/// Выплёскивает кровь из кровотока наружу, например при кашле кровью.
/// </summary>
public sealed partial class BleedOut : EntityEffectBase<BleedOut>
{
    /// <summary>
    /// Сколько крови выплеснуть.
    /// </summary>
    [DataField]
    public FixedPoint2 Amount = 10f;

    /// <summary>
    /// Звук, с которым кровь выплёскивается.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-bleed-out", ("chance", Probability), ("amount", Amount.Float()));
}
