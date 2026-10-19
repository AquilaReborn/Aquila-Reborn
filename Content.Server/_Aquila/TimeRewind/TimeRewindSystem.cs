using Content.Shared.Aquila.TimeRewind;
using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Aquila.TimeRewind;

public sealed class TimeRewindSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimeRewindComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TimeRewindComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TimeRewindComponent, TimeRewindActionEvent>(OnRewindAction);
    }

    private void OnMapInit(Entity<TimeRewindComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.ActionId);
    }

    private void OnShutdown(Entity<TimeRewindComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnRewindAction(Entity<TimeRewindComponent> ent, ref TimeRewindActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var (uid, comp) = ent;
        var originXform = Transform(uid);

        if (HasComp<PendingRewindComponent>(uid))
            return;

        var pending = AddComp<PendingRewindComponent>(uid);
        pending.EndTime = _timing.CurTime + comp.RewindDelay;
        pending.EffectDuration = comp.EffectDuration;

        var visitedForItems = new HashSet<EntityUid>();

        void CapturePosition(EntityUid target, TransformComponent xform)
        {
            if (_container.IsEntityInContainer(target))
                return;

            EntityUid? buckledTo = null;
            if (TryComp<BuckleComponent>(target, out var buckle) && buckle.Buckled)
                buckledTo = buckle.BuckledTo;

            pending.Snapshots.Add(new RewindSnapshot
            {
                Entity = target,
                Coordinates = xform.Coordinates,
                Rotation = xform.LocalRotation,
                BuckledTo = buckledTo,
            });
        }

        CapturePosition(uid, originXform);
        CaptureItems(uid, pending, visitedForItems);

        foreach (var target in _lookup.GetEntitiesInRange(originXform.Coordinates, comp.Radius))
        {
            if (target == uid)
                continue;

            if (!TryComp<TransformComponent>(target, out var targetXform) || targetXform.Anchored)
                continue;

            if (HasComp<MapGridComponent>(target) || HasComp<MapComponent>(target))
                continue;

            CapturePosition(target, targetXform);
            CaptureItems(target, pending, visitedForItems);
        }
    }

    private void CaptureItems(EntityUid owner, PendingRewindComponent pending, HashSet<EntityUid> visited)
    {
        if (!visited.Add(owner))
            return;

        if (HasComp<InventoryComponent>(owner) && _inventory.TryGetSlots(owner, out var slotDefs))
        {
            foreach (var slot in slotDefs)
            {
                if (!_inventory.TryGetSlotEntity(owner, slot.Name, out var item))
                    continue;

                pending.Items.Add(new ItemSnapshot
                {
                    Item = item.Value,
                    Kind = ItemContainerKind.Inventory,
                    Owner = owner,
                    SlotOrHand = slot.Name,
                });

                CaptureItems(item.Value, pending, visited);
            }
        }

        if (TryComp<HandsComponent>(owner, out var hands))
        {
            foreach (var handId in _hands.EnumerateHands((owner, hands)))
            {
                var held = _hands.GetHeldItem((owner, hands), handId);
                if (held == null)
                    continue;

                pending.Items.Add(new ItemSnapshot
                {
                    Item = held.Value,
                    Kind = ItemContainerKind.Hand,
                    Owner = owner,
                    SlotOrHand = handId,
                });

                CaptureItems(held.Value, pending, visited);
            }
        }

        if (TryComp<StorageComponent>(owner, out var storage) && storage.Container != null)
        {
            foreach (var contained in storage.Container.ContainedEntities)
            {
                pending.Items.Add(new ItemSnapshot
                {
                    Item = contained,
                    Kind = ItemContainerKind.Storage,
                    Owner = owner,
                    StorageContainer = storage.Container,
                });

                CaptureItems(contained, pending, visited);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var pendingQuery = EntityQueryEnumerator<PendingRewindComponent>();
        while (pendingQuery.MoveNext(out var uid, out var pending))
        {
            if (now < pending.EndTime)
                continue;

            RestoreItems(pending);
            RestorePositions(pending);
            RemCompDeferred<PendingRewindComponent>(uid);
        }

        var effectQuery = EntityQueryEnumerator<RewindedComponent>();
        while (effectQuery.MoveNext(out var uid, out var rewinded))
        {
            if (now >= rewinded.EndTime)
                RemCompDeferred<RewindedComponent>(uid);
        }
    }

    private void RestorePositions(PendingRewindComponent pending)
    {
        foreach (var snap in pending.Snapshots)
        {
            if (!Exists(snap.Entity) || TerminatingOrDeleted(snap.Entity))
                continue;

            if (TryComp<BuckleComponent>(snap.Entity, out var buckle)
                && buckle.Buckled
                && buckle.BuckledTo != snap.BuckledTo)
            {
                _buckle.TryUnbuckle(snap.Entity, snap.Entity, true);
            }

            if (_container.TryGetContainingContainer((snap.Entity, null, null), out var currentContainer))
                _container.Remove(snap.Entity, currentContainer, force: true);

            _transform.SetCoordinates(snap.Entity, snap.Coordinates);
            _transform.SetLocalRotation(snap.Entity, snap.Rotation);

            var rewinded = EnsureComp<RewindedComponent>(snap.Entity);
            rewinded.EndTime = _timing.CurTime + pending.EffectDuration;
            rewinded.Duration = pending.EffectDuration;
            Dirty(snap.Entity, rewinded);
        }
    }

    private void RestoreItems(PendingRewindComponent pending)
    {
        foreach (var snap in pending.Items)
        {
            if (!Exists(snap.Item) || TerminatingOrDeleted(snap.Item))
                continue;

            switch (snap.Kind)
            {
                case ItemContainerKind.Inventory:
                {
                    if (!Exists(snap.Owner) || TerminatingOrDeleted(snap.Owner) || snap.SlotOrHand == null)
                        continue;

                    if (_inventory.TryGetSlotEntity(snap.Owner, snap.SlotOrHand, out var current) && current == snap.Item)
                        continue;

                    _inventory.TryUnequip(snap.Owner, snap.SlotOrHand, out _, force: true);
                    _inventory.TryEquip(snap.Owner, snap.Item, snap.SlotOrHand, force: true);
                    break;
                }

                case ItemContainerKind.Hand:
                    {
                        if (!Exists(snap.Owner) || TerminatingOrDeleted(snap.Owner) || snap.SlotOrHand == null)
                            continue;

                        if (!TryComp<HandsComponent>(snap.Owner, out var hands))
                            continue;

                        var currentHeld = _hands.GetHeldItem((snap.Owner, hands), snap.SlotOrHand);

                        if (currentHeld == snap.Item)
                            continue;

                        if (currentHeld != null)
                            _hands.TryDrop((snap.Owner, hands), snap.SlotOrHand);

                        _hands.TryPickup(snap.Owner, snap.Item, snap.SlotOrHand, handsComp: hands);
                        break;
                    }

                case ItemContainerKind.Storage:
                {
                    if (snap.StorageContainer == null)
                        continue;

                    if (snap.StorageContainer.Contains(snap.Item))
                        continue;

                    _container.Insert(snap.Item, snap.StorageContainer, force: true);
                    break;
                }
            }
        }
    }
}
