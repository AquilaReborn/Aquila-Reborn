using Content.Goobstation.Common.Religion;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Events;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Лунный амулет обращает достаточно обезумевшего члена экипажа в союзника еретика пути Луны.
/// </summary>
public sealed class MoonAmuletSystem : SharedMoonAmuletSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MoonAscensionSystem _ascension = default!;
    [Dependency] private readonly MoonSanitySystem _sanity = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    private static readonly LocId ConversionFailedMessage = "moon-amulet-conversion-failed";
    private static readonly LocId NotMadMessage = "moon-amulet-not-mad";
    private static readonly LocId ConversionLimitMessage = "moon-amulet-limit";
    private static readonly EntProtoId MindGateStatus = "StatusEffectMoonMindGate";

    /// <summary>
    /// Подключает применение удерживаемого амулета к цели.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MoonAmuletComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MoonAmuletConversionDoAfterEvent>(OnConversionDoAfter);
    }

    private void OnAfterInteract(Entity<MoonAmuletComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        args.Handled = TryConvertWithAmulet((ent.Owner, ent.Comp), args.User, target);
    }

    /// <summary>
    /// Обращает подходящую цель амулетом или запускает обращение цели со щитом разума через дуафтер,
    /// тратя заряд и кулдаун только после успешной выдачи роли.
    /// </summary>
    public bool TryConvertWithAmulet(Entity<MoonAmuletComponent?> ent, EntityUid user, EntityUid target, bool quiet = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!CanConvertWithAmulet((ent.Owner, ent.Comp), user, target, quiet))
            return false;

        if (DoConvertWithAmulet((ent.Owner, ent.Comp), user, target))
            return true;

        if (!quiet)
            _popup.PopupEntity(Loc.GetString(ConversionFailedMessage), ent.Owner, user);
        return false;
    }

    private bool CanConvertWithAmulet(Entity<MoonAmuletComponent> ent, EntityUid user, EntityUid target, bool quiet = false)
    {
        if (user == target || TerminatingOrDeleted(ent.Owner) || EntityManager.IsQueuedForDeletion(ent.Owner) ||
            TerminatingOrDeleted(user) || TerminatingOrDeleted(target) ||
            _timing.CurTime < ent.Comp.NextUse || !_hands.IsHolding(user, ent.Owner) ||
            !_interaction.InRangeUnobstructed(user, target) ||
            !_heretic.TryGetHereticComponent(user, out var heretic, out _) || heretic.CurrentPath != ent.Comp.RequiredPath)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString(ConversionFailedMessage), ent.Owner, user);
            return false;
        }

        var denyEvent = new BeforeCastTouchSpellEvent(target, doEffects: !quiet);
        RaiseLocalEvent(target, denyEvent, true);
        if (denyEvent.Cancelled)
            return false;

        if (ent.Comp.Conversions >= ent.Comp.MaxConversions)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString(ConversionLimitMessage), ent.Owner, user);
            return false;
        }

        if (_sanity.IsInsane(target, ent.Comp.ConversionThreshold) ||
            _status.HasStatusEffect(target, MoonSanitySystem.InsanityStatus) ||
            _status.HasStatusEffect(target, MindGateStatus))
            return true;

        if (!quiet)
            _popup.PopupEntity(Loc.GetString(NotMadMessage), ent.Owner, user);
        return false;
    }

    private bool DoConvertWithAmulet(Entity<MoonAmuletComponent> ent, EntityUid user, EntityUid target)
    {
        if (!HasComp<MindShieldComponent>(target))
            return FinishConversion(ent, user, target, false);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, ent.Comp.MindShieldConversionTime,
            new MoonAmuletConversionDoAfterEvent(), target, target: target, used: ent.Owner)
        {
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            Broadcast = true,
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private bool FinishConversion(Entity<MoonAmuletComponent> ent, EntityUid user, EntityUid target, bool ignoreMindShield)
    {
        if (!_ascension.TryConvertLunatic((target, null), user, 0f,
                ent.Comp.Effect, ent.Comp.LunaticAmulet, ignoreMindShield))
            return false;

        ent.Comp.Conversions++;
        ent.Comp.NextUse = _timing.CurTime + ent.Comp.UseDelay;
        return true;
    }

    private void OnConversionDoAfter(MoonAmuletConversionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target || args.Used is not { } used)
            return;

        if (!TryComp<MoonAmuletComponent>(used, out var comp) || TerminatingOrDeleted(target))
            return;

        if (!CanConvertWithAmulet((used, comp), args.User, target, quiet: true))
            return;

        FinishConversion((used, comp), args.User, target, ignoreMindShield: true);
        args.Handled = true;
    }
}
