using System.Linq;
using Content.Shared._Aquila.Heretic.Lock.Components;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Systems;
using Robust.Server.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Heretic.Lock.Systems;

/// <summary>
/// Обслуживает карту Хранителя ключей: ставит пару порталов, гасит предыдущую пару и поглощает чужие ID-карты.
/// </summary>
public sealed class EldritchCardSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;

    private static readonly LocId CardFirstLinkMessage = "heretic-lock-card-first-link";
    private static readonly LocId CardOtherMapMessage = "heretic-lock-card-other-map";
    private static readonly LocId CardNotHereticMessage = "heretic-lock-card-not-heretic";
    private static readonly LocId CardConsumedMessage = "heretic-lock-card-consumed";
    private static readonly LocId CardLinkFailedMessage = "heretic-lock-card-link-failed";
    private static readonly LocId CardLinkedMessage = "heretic-lock-card-linked";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EldritchCardComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<EldritchCardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EldritchCardComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<EldritchCardComponent> ent, ref ComponentShutdown args)
    {
        ClearPortals(ent);
    }

    private void OnInteractUsing(Entity<EldritchCardComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<IdCardComponent>(args.Used))
            return;

        if (!CanUseCard(args.User))
            return;

        if (TryConsumeCard(ent, args.Used, args.User))
            args.Handled = true;
    }

    private void OnAfterInteract(Entity<EldritchCardComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!CanUseCard(args.User))
            return;

        if (args.Target is { } target && !TerminatingOrDeleted(target) && HasComp<IdCardComponent>(target))
        {
            if (TryConsumeCard(ent, target, args.User))
                args.Handled = true;

            return;
        }

        var coords = args.Target is { } other && !TerminatingOrDeleted(other)
            ? Transform(other).Coordinates
            : args.ClickLocation;

        if (!coords.IsValid(EntityManager))
            return;

        args.Handled = true;

        if (ent.Comp.PendingCoords is not { } first)
        {
            ent.Comp.PendingCoords = coords;
            _popup.PopupEntity(Loc.GetString(CardFirstLinkMessage), args.User, args.User);
            return;
        }

        ent.Comp.PendingCoords = null;

        if (_transform.GetMapId(first) != _transform.GetMapId(coords))
        {
            _popup.PopupEntity(Loc.GetString(CardOtherMapMessage), args.User, args.User);
            return;
        }

        CreatePair(ent, first, coords, args.User);
    }

    private bool CanUseCard(EntityUid user, bool quiet = false)
    {
        if (_heretic.TryGetHereticComponent(user, out _, out _))
            return true;

        if (!quiet)
            _popup.PopupEntity(Loc.GetString(CardNotHereticMessage), user, user);

        return false;
    }

    private bool TryConsumeCard(Entity<EldritchCardComponent> ent, EntityUid card, EntityUid user)
    {
        if (card == ent.Owner || TerminatingOrDeleted(card))
            return false;

        if (!TryComp(card, out IdCardComponent? cardId))
            return false;

        var cardName = Name(card);

        if (_access.TryGetTags(card) is { } cardTags && _access.TryGetTags(ent.Owner) is { } ownTags)
            _access.TrySetTags(ent.Owner, ownTags.Union(cardTags).ToList());

        if (TryComp(ent.Owner, out IdCardComponent? ownId))
            CopyIdentity(ent.Owner, cardId, ownId);

        if (_container.TryGetContainingContainer((card, Transform(card), null), out var container))
            _container.Remove(card, container, force: true);

        QueueDel(card);

        _popup.PopupEntity(Loc.GetString(CardConsumedMessage, ("card", cardName)), user, user);
        return true;
    }

    private void CopyIdentity(EntityUid uid, IdCardComponent source, IdCardComponent target)
    {
        _idCard.TryChangeFullName(uid, source.FullName, target);

        target.JobTitle = source.JobTitle;
        Dirty(uid, target);

        _idCard.TryChangeJobDepartment(uid, source.JobDepartments, target);

        if (_proto.TryIndex(source.JobIcon, out var jobIcon))
            _idCard.TryChangeJobIcon(uid, jobIcon, target);
    }

    private void CreatePair(Entity<EldritchCardComponent> ent,
        EntityCoordinates firstCoords,
        EntityCoordinates secondCoords,
        EntityUid user)
    {
        ClearPortals(ent);

        var firstPortal = SpawnAtPosition(ent.Comp.PortalPrototype, firstCoords);
        var secondPortal = SpawnAtPosition(ent.Comp.PortalPrototype, secondCoords);

        if (!_link.TryLink(firstPortal, secondPortal, true))
        {
            QueueDel(firstPortal);
            QueueDel(secondPortal);
            _popup.PopupEntity(Loc.GetString(CardLinkFailedMessage), user, user);
            return;
        }

        ent.Comp.FirstPortal = firstPortal;
        ent.Comp.SecondPortal = secondPortal;

        _popup.PopupEntity(Loc.GetString(CardLinkedMessage), user, user);
    }

    private void ClearPortals(Entity<EldritchCardComponent> ent)
    {
        if (ent.Comp.FirstPortal is { } first && !TerminatingOrDeleted(first))
            QueueDel(first);

        if (ent.Comp.SecondPortal is { } second && !TerminatingOrDeleted(second))
            QueueDel(second);

        ent.Comp.FirstPortal = null;
        ent.Comp.SecondPortal = null;
    }
}
