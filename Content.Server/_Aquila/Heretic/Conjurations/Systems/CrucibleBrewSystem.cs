using Content.Server._Aquila.Heretic.Conjurations.Components;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Aquila.Heretic.Conjurations.Systems;

/// <summary>
/// Применяет зелья Зубастого тигля: выдаёт статус-эффект своему, травит чужого и удаляет зелье.
/// </summary>
public sealed class CrucibleBrewSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private static readonly LocId DrunkMessage = "mawed-crucible-brew-drunk";
    private static readonly LocId RejectMessage = "mawed-crucible-brew-reject";
    private static readonly LocId CooldownMessage = "mawed-crucible-brew-cooldown";

    /// <summary>
    /// Подключает употребление зелья из руки.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrucibleBrewComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<CrucibleBrewComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.User;

        if (ent.Comp.RequireHeretic && !_heretic.IsHereticOrGhoul(user))
        {
            _popup.PopupEntity(Loc.GetString(RejectMessage), ent.Owner, user);
            _damageable.TryChangeDamage(user, ent.Comp.RejectDamage, origin: ent.Owner);
            QueueDel(ent);
            return;
        }

        if (ent.Comp.BlockedByStatus is { } blocked && _status.HasStatusEffect(user, blocked))
        {
            _popup.PopupEntity(Loc.GetString(CooldownMessage), ent.Owner, user);
            return;
        }

        var name = Name(ent);
        _status.TryAddStatusEffectDuration(user, ent.Comp.StatusEffect, ent.Comp.Duration);
        _audio.PlayPvs(ent.Comp.DrinkSound, user);
        _popup.PopupEntity(Loc.GetString(DrunkMessage, ("brew", name)), ent.Owner, user);
        QueueDel(ent);
    }
}
