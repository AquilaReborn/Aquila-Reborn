using System.Diagnostics.CodeAnalysis;
using Content.Shared._Aquila.Language.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Aquila.Language;

/// <summary>
/// Вставка языковых чипов в языковой имплант и их извлечение.
/// </summary>
public abstract class SharedTranslatorChipSystem : EntitySystem
{
    private static readonly LocId NoImplantMessage = "translator-chip-no-implant";
    private static readonly LocId FullMessage = "translator-chip-full";
    private static readonly LocId DuplicateMessage = "translator-chip-duplicate";
    private static readonly LocId InsertedMessage = "translator-chip-inserted";
    private static readonly LocId InsertingOtherMessage = "translator-chip-inserting-other";
    private static readonly LocId RemoveVerb = "translator-chip-verb-remove";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TranslatorChipHolderComponent, ComponentInit>(OnHolderInit);
        SubscribeLocalEvent<TranslatorChipComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TranslatorChipComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TranslatorChipComponent, TranslatorChipInsertDoAfterEvent>(OnInsertDoAfter);
        SubscribeLocalEvent<ImplantedComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    /// <summary>
    /// Ищет языковой имплант в теле.
    /// </summary>
    public bool TryGetHolder(EntityUid body, [NotNullWhen(true)] out Entity<TranslatorChipHolderComponent>? holder)
    {
        holder = null;
        if (!_container.TryGetContainer(body, ImplanterComponent.ImplantSlotId, out var implants))
            return false;

        foreach (var implant in implants.ContainedEntities)
        {
            if (!TryComp<TranslatorChipHolderComponent>(implant, out var comp))
                continue;

            holder = (implant, comp);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Начинает вставку чипа в языковой имплант цели. Возвращает false, если вставить нельзя.
    /// </summary>
    public bool TryStartInsert(Entity<TranslatorChipComponent> chip, EntityUid user, EntityUid target)
    {
        if (!CanInsert(chip, user, target, out var holder))
            return false;

        var doAfter = new DoAfterArgs(EntityManager, user, holder.Value.Comp.InsertDelay, new TranslatorChipInsertDoAfterEvent(), chip, target: target, used: chip)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        if (user != target)
            _popup.PopupEntity(Loc.GetString(InsertingOtherMessage, ("user", Identity.Entity(user, EntityManager))), target, target, PopupType.Medium);

        return true;
    }

    private bool CanInsert(Entity<TranslatorChipComponent> chip, EntityUid user, EntityUid target, [NotNullWhen(true)] out Entity<TranslatorChipHolderComponent>? holder)
    {
        if (!TryGetHolder(target, out holder))
        {
            _popup.PopupClient(Loc.GetString(NoImplantMessage), target, user);
            return false;
        }

        var chips = GetChips(holder.Value);
        if (chips.ContainedEntities.Count >= holder.Value.Comp.MaxChips)
        {
            _popup.PopupClient(Loc.GetString(FullMessage), target, user);
            return false;
        }

        var proto = MetaData(chip).EntityPrototype?.ID;
        foreach (var inserted in chips.ContainedEntities)
        {
            if (proto == null || MetaData(inserted).EntityPrototype?.ID != proto)
                continue;

            _popup.PopupClient(Loc.GetString(DuplicateMessage), target, user);
            return false;
        }

        return true;
    }

    private BaseContainer GetChips(Entity<TranslatorChipHolderComponent> holder)
    {
        return _container.EnsureContainer<Container>(holder, TranslatorChipHolderComponent.ContainerId);
    }

    private void OnHolderInit(Entity<TranslatorChipHolderComponent> ent, ref ComponentInit args)
    {
        GetChips(ent);
    }

    private void OnUseInHand(Entity<TranslatorChipComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartInsert(ent, args.User, args.User);
    }

    private void OnAfterInteract(Entity<TranslatorChipComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not { } target
            || !HasComp<ImplantedComponent>(target))
            return;

        args.Handled = TryStartInsert(ent, args.User, target);
    }

    private void OnInsertDoAfter(Entity<TranslatorChipComponent> ent, ref TranslatorChipInsertDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Handled
            || args.Target is not { } target
            || !CanInsert(ent, args.User, target, out var holder))
            return;

        args.Handled = true;

        if (!_container.Insert(ent.Owner, GetChips(holder.Value)))
            return;

        _audio.PlayPredicted(holder.Value.Comp.InsertSound, target, args.User);
        _popup.PopupClient(Loc.GetString(InsertedMessage, ("chip", ent.Owner)), target, args.User);
    }

    private void OnGetVerbs(Entity<ImplantedComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (args.User != args.Target
            || !args.CanAccess
            || !TryGetHolder(ent, out var holder))
            return;

        var user = args.User;
        var sound = holder.Value.Comp.InsertSound;
        var chips = GetChips(holder.Value);
        foreach (var chip in chips.ContainedEntities)
        {
            var target = chip;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(RemoveVerb, ("chip", target)),
                Category = VerbCategory.Eject,
                Act = () =>
                {
                    if (!_container.Remove(target, chips))
                        return;

                    _hands.PickupOrDrop(user, target);
                    _audio.PlayPredicted(sound, user, user);
                },
            });
        }
    }
}
