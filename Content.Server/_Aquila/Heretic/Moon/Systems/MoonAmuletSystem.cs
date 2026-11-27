using Content.Server._Aquila.Heretic.Moon.Components;
using Content.Server.Heretic.EntitySystems;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using StatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Лунный амулет: бьёт по рассудку цели, вгоняет безумцев в буйство и проводит урон через лунный клинок носителя.
/// </summary>
public sealed class MoonAmuletSystem : SharedMoonAmuletSystem
{
    [Dependency] private readonly HereticBladeSystem _blades = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MoonSanitySystem _sanity = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoonAmuletComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MoonAmuletComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MoonAmuletComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<MoonAmuletComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnEquipped(Entity<MoonAmuletComponent> ent, ref GotEquippedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Equipee) && !HasComp<MoonLunaticComponent>(args.Equipee))
            return;

        ent.Comp.Wearer = args.Equipee;

        var wearer = EnsureComp<MoonAmuletWearerComponent>(args.Equipee);
        wearer.BrainHealMultiplier = ent.Comp.BrainHealMultiplier;
        wearer.ChannelBrainMultiplier = ent.Comp.ChannelBrainMultiplier;
        wearer.ChannelInsanityDuration = ent.Comp.ChannelInsanityDuration;
        wearer.ChannelEffect = ent.Comp.Effect;

        if (!TryComp<HandsComponent>(args.Equipee, out var hands))
            return;

        foreach (var held in _hands.EnumerateHeld((args.Equipee, hands)))
        {
            if (TryComp<HereticBladeComponent>(held, out var blade))
                _blades.ChannelMoonBlade(held, wearer, blade);
        }
    }

    private void OnUnequipped(Entity<MoonAmuletComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.Wearer = null;
        RemoveWearer(args.Equipee);
    }

    private void OnShutdown(Entity<MoonAmuletComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Wearer is not { } wearer || TerminatingOrDeleted(wearer))
            return;

        RemoveWearer(wearer);
    }

    private void RemoveWearer(EntityUid wearer)
    {
        if (TerminatingOrDeleted(wearer))
            return;

        if (TryComp<HandsComponent>(wearer, out var hands))
        {
            foreach (var held in _hands.EnumerateHeld((wearer, hands)))
            {
                if (TryComp<HereticBladeComponent>(held, out var blade))
                    _blades.RestoreMoonChannel(held, blade);
            }
        }

        RemComp<MoonAmuletWearerComponent>(wearer);
    }

    private void OnAfterInteract(Entity<MoonAmuletComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || target == args.User || !args.CanReach)
            return;

        if (!HasComp<MobStateComponent>(target) || !HasComp<StatusEffectsComponent>(target))
            return;

        if (_heretic.IsHereticOrGhoul(target))
            return;

        if (_timing.CurTime < ent.Comp.NextUse)
            return;

        ent.Comp.NextUse = _timing.CurTime + ent.Comp.UseDelay;
        args.Handled = true;

        _audio.PlayPvs(ent.Comp.LaughSound, args.User);
        SpawnAtPosition(ent.Comp.Effect, Transform(target).Coordinates);

        _sanity.ApplyBrainDamage(target, ent.Comp.Damage);

        if (!_sanity.IsInsane(target, ent.Comp.InsaneThreshold))
        {
            _status.TryAddStatusEffectDuration(target,
                MoonSanitySystem.InsanityStatus,
                ent.Comp.HallucinationDuration);
            return;
        }

        _sanity.ApplyBrainDamage(target, ent.Comp.InsaneDamage);
        _stun.KnockdownOrStun(target, ent.Comp.StunDuration, true);
        _status.TryAddStatusEffectDuration(target,
            MoonSanitySystem.InsanityStatus,
            ent.Comp.HallucinationDuration);
    }
}
