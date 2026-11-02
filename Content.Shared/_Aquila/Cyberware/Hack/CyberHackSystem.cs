using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Aquila.Cyberware.Hack;

/// <summary>
/// Имплант взлома: две секунды тянет к устройству цифровое полотно и взламывает его как емаг,
/// сразу и доступ, и скрытые функции. Каждый взлом бьёт по здоровью и нагружает организм.
/// </summary>
public sealed class CyberHackSystem : EntitySystem
{
    private static readonly LocId CostMessage = "cyber-hack-cost";
    private static readonly LocId FailedMessage = "cyber-hack-failed";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberHackImplantComponent, CyberHackActionEvent>(OnAction);
        SubscribeLocalEvent<CyberHackImplantComponent, CyberHackDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// Начинает взлом устройства имплантом.
    /// </summary>
    public bool TryStartHack(Entity<CyberHackImplantComponent?> implant, EntityUid user, EntityUid target)
    {
        if (!Resolve(implant, ref implant.Comp))
            return false;

        var doAfter = new DoAfterArgs(EntityManager, user, implant.Comp.HackTime, new CyberHackDoAfterEvent(), implant.Owner, target: target, used: implant.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            DistanceThreshold = implant.Comp.Range,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        var beam = EnsureComp<CyberHackBeamComponent>(user);
        beam.Target = target;
        Dirty(user, beam);

        _audio.PlayPredicted(implant.Comp.StartSound, user, user);
        return true;
    }

    private void OnAction(Entity<CyberHackImplantComponent> ent, ref CyberHackActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartHack(ent.AsNullable(), args.Performer, args.Target);
    }

    private void OnDoAfter(Entity<CyberHackImplantComponent> ent, ref CyberHackDoAfterEvent args)
    {
        RemComp<CyberHackBeamComponent>(args.User);

        if (args.Cancelled
            || args.Handled
            || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!_emag.TryEmagEffect(ent.Owner, args.User, target))
        {
            _popup.PopupClient(Loc.GetString(FailedMessage), args.User, args.User);
            return;
        }

        _damageable.TryChangeDamage(args.User, ent.Comp.Damage, ignoreResistances: true, origin: args.User);
        _popup.PopupClient(Loc.GetString(CostMessage), args.User, args.User, PopupType.MediumCaution);

        var strain = new CyberwareStrainEvent(ent.Comp.Strain);
        RaiseLocalEvent(args.User, ref strain);
    }
}
