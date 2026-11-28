using Content.Server._Aquila.Heretic.Conjurations.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Implants;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Aquila.Heretic.Conjurations.Systems;

/// <summary>
/// Применяет Эфир Новорождённого: полностью лечит пользователя, убирает импланты и погружает его в сон.
/// </summary>
public sealed class EtherOfTheNewbornSystem : EntitySystem
{
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EtherOfTheNewbornComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<EtherOfTheNewbornComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !HasComp<MobStateComponent>(args.User))
            return;

        args.Handled = true;

        _rejuvenate.PerformRejuvenate(args.User);
        _implants.WipeImplants(args.User);
        _status.TryAddStatusEffectDuration(args.User, SleepingSystem.StatusEffectForcedSleeping, ent.Comp.SleepDuration);
        _audio.PlayPvs(ent.Comp.DrinkSound, args.User);
        QueueDel(ent);
    }
}
