using Content.Shared._Aquila.Heretic.Lock.Events;
using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Polymorph;
using Content.Shared.Storage;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<PolymorphPrototype> LockRefugePolymorph = "HereticLockRefuge";

    private static readonly LocId BurglarSuccessPopup = "heretic-lock-burglar-success";
    private static readonly LocId BurglarFullHandsPopup = "heretic-lock-burglar-full-hands";
    private static readonly LocId BurglarVictimPopup = "heretic-lock-burglar-victim";
    private static readonly LocId BurglarNoStoragePopup = "heretic-lock-burglar-no-storage";
    private static readonly LocId RefugeCrowdedPopup = "heretic-lock-refuge-crowded";

    private const string BackSlot = "back";
    private const string LockPath = "Lock";

    private void InitializeAquila()
    {
        SubscribeLocalEvent<EventHereticBulglarFinesse>(OnBurglarFinesse);
        SubscribeLocalEvent<EventHereticLastRefugee>(OnLastRefugee);
        SubscribeLocalEvent<EventHereticLabyrinthBarrier>(OnLabyrinthBarrier);
        SubscribeLocalEvent<EventHereticShapeshift>(OnAscendedShapeshift);
    }

    private void OnBurglarFinesse(EventHereticBulglarFinesse args)
    {
        if (!TryUseAbility(args))
            return;

        var target = args.Target;
        var user = args.Performer;

        if (target == user)
            return;

        if (!_inventory.TryGetSlotEntity(target, BackSlot, out var backItem) ||
            !_container.TryGetContainer(backItem.Value, StorageComponent.ContainerId, out var container))
        {
            ShowNoStoragePopup(user);
            return;
        }

        var items = new List<EntityUid>(container.ContainedEntities);
        if (items.Count == 0)
        {
            ShowNoStoragePopup(user);
            return;
        }

        var item = _random.Pick(items);

        if (!_container.Remove(item, container, force: true))
            return;

        _transform.SetCoordinates(item, Transform(user).Coordinates);

        if (_hands.TryPickupAnyHand(user, item, animate: false))
            Popup.PopupEntity(Loc.GetString(BurglarSuccessPopup, ("item", Name(item))), user, user);
        else
            Popup.PopupEntity(Loc.GetString(BurglarFullHandsPopup, ("item", Name(item))), user, user);

        Popup.PopupEntity(Loc.GetString(BurglarVictimPopup), target, target);
    }

    private void OnLastRefugee(EventHereticLastRefugee args)
    {
        if (!TryUseAbility(args))
            return;

        var user = args.Performer;

        foreach (var (other, _) in Lookup.GetEntitiesInRange<MobStateComponent>(Transform(user).Coordinates,
                     args.SearchRadius,
                     LookupFlags.Dynamic))
        {
            if (other == user || !_mobstate.IsAlive(other) || !HasComp<ActorComponent>(other))
                continue;

            Popup.PopupEntity(Loc.GetString(RefugeCrowdedPopup), user, user);
            return;
        }

        _poly.PolymorphEntity(user, LockRefugePolymorph);
    }

    private void OnLabyrinthBarrier(EventHereticLabyrinthBarrier args)
    {
        if (!TryUseAbility(args))
            return;

        if (!args.Target.IsValid(EntityManager))
            return;

        SpawnAtPosition(args.Barrier, args.Target);
    }

    private void OnAscendedShapeshift(EventHereticShapeshift args)
    {
        if (args.Handled || HasComp<GhoulComponent>(args.Performer))
            return;

        if (!CanShapeshift(args.Performer) || !HasComp<ShapeshiftActionComponent>(args.Action))
            return;

        _ui.TryOpenUi(args.Action.Owner, HereticShapeshiftUiKey.Key, args.Performer);
    }

    private void ShowNoStoragePopup(EntityUid user)
    {
        Popup.PopupEntity(Loc.GetString(BurglarNoStoragePopup), user, user);
    }

    private bool CanShapeshift(EntityUid user)
    {
        if (HasComp<GhoulComponent>(user))
            return true;

        return Heretic.TryGetHereticComponent(user, out var heretic, out _) &&
               heretic is { Ascended: true, CurrentPath: LockPath };
    }

    private bool CanTransferPolymorphDamage(ProtoId<PolymorphPrototype> polymorph)
    {
        return _prototypes.Index(polymorph).Configuration.TransferDamage;
    }
}
