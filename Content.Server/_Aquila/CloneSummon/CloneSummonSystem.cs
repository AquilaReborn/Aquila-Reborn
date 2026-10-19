using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC;
using Content.Shared.Aquila.Clone;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Damage;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Points;

namespace Content.Server.Aquila.Clone;

public sealed class CloneSummonSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private const string HostileHtnTask = "SimpleHumanoidHostileCompound";
    private const string HostileFaction = "Syndicate";

    private static readonly EntProtoId TeleportActionId = "ActionTeleportToClone";
    private static readonly FixedPoint2 TeleportHealAmount = FixedPoint2.New(20);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SummonCloneActionEvent>(OnSummonClone);
        SubscribeLocalEvent<CloneSummonerComponent, TeleportToCloneActionEvent>(OnTeleportToClone);

        SubscribeLocalEvent<CloneSummonComponent, MobStateChangedEvent>(OnCloneMobStateChanged);
        SubscribeLocalEvent<CloneSummonComponent, AttackAttemptEvent>(OnCloneAttackAttempt);
        SubscribeLocalEvent<CloneSummonComponent, ComponentShutdown>(OnCloneComponentShutdown);

        SubscribeLocalEvent<CloneSummonerActionComponent, ComponentStartup>(OnSummonerActionStartup);
        SubscribeLocalEvent<CloneSummonerActionComponent, ComponentShutdown>(OnSummonerActionShutdown);
    }

    private void OnSummonerActionStartup(EntityUid uid, CloneSummonerActionComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
    }

    private void OnSummonerActionShutdown(EntityUid uid, CloneSummonerActionComponent comp, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnSummonClone(SummonCloneActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (target == EntityUid.Invalid || !Exists(target))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (!TryComp<MetaDataComponent>(target, out var targetMeta) || targetMeta.EntityPrototype == null)
            return;

        args.Handled = true;

        var performer = args.Performer;
        var spawnCoords = Transform(target).Coordinates;

        var clone = Spawn(targetMeta.EntityPrototype.ID, spawnCoords);

        _humanoidAppearance.CloneAppearance(target, clone);
        _metaData.SetEntityName(clone, targetMeta.EntityName);

        CloneEquipment(target, clone);

        var cloneComp = EnsureComp<CloneSummonComponent>(clone);
        cloneComp.Summoner = performer;
        cloneComp.TimeToLive = 30f;

        cloneComp.OriginalItems = CollectCurrentItems(clone);

        if (TryComp<CloneSummonerActionComponent>(performer, out var summonerActionComp))
        {
            cloneComp.DespawnEffect = summonerActionComp.SpawnEffect;

            if (summonerActionComp.SpawnEffect != null)
                Spawn(summonerActionComp.SpawnEffect, spawnCoords);
        }

        EnsureComp<ActiveNPCComponent>(clone);

        var htn = new HTNComponent
        {
            RootTask = new HTNCompoundTask { Task = HostileHtnTask }
        };
        EntityManager.AddComponent(clone, htn, overwrite: true);

        _faction.ClearFactions(clone, dirty: false);
        _faction.AddFaction(clone, HostileFaction);
        _faction.IgnoreEntity(clone, performer);

        var summonerComp = EnsureComp<CloneSummonerComponent>(performer);
        summonerComp.Clones.Add(clone);

        if (summonerComp.TeleportActionEntity == null)
        {
            EntityUid? actionEnt = null;
            _actions.AddAction(performer, ref actionEnt, TeleportActionId);
            summonerComp.TeleportActionEntity = actionEnt;
        }
    }

    private void CloneEquipment(EntityUid source, EntityUid target)
    {
        if (_inventory.TryGetSlots(source, out var slotDefs))
        {
            foreach (var slot in slotDefs)
            {
                if (!_inventory.TryGetSlotEntity(source, slot.Name, out var itemUid))
                    continue;

                if (!TryComp<MetaDataComponent>(itemUid.Value, out var itemMeta) || itemMeta.EntityPrototype == null)
                    continue;

                var newItem = Spawn(itemMeta.EntityPrototype.ID, Transform(target).Coordinates);
                if (!_inventory.TryEquip(target, newItem, slot.Name, force: true, silent: true))
                    QueueDel(newItem);
            }
        }

        if (HasComp<HandsComponent>(source))
        {
            foreach (var held in _hands.EnumerateHeld(source))
            {
                if (!TryComp<MetaDataComponent>(held, out var itemMeta) || itemMeta.EntityPrototype == null)
                    continue;

                var newItem = Spawn(itemMeta.EntityPrototype.ID, Transform(target).Coordinates);
                if (!_hands.TryPickup(target, newItem))
                    QueueDel(newItem);
            }
        }
    }

    private List<EntityUid> CollectCurrentItems(EntityUid ent)
    {
        var items = new List<EntityUid>();

        if (TryComp<InventoryComponent>(ent, out var inventory)
            && _inventory.TryGetContainerSlotEnumerator((ent, inventory), out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (_inventory.TryGetSlotEntity(ent, slot.ID, out var itemUid))
                    items.Add(itemUid.Value);
            }
        }

        if (HasComp<HandsComponent>(ent))
            items.AddRange(_hands.EnumerateHeld(ent));

        return items;
    }

    private void OnTeleportToClone(EntityUid uid, CloneSummonerComponent comp, TeleportToCloneActionEvent args)
    {
        if (args.Handled)
            return;

        while (comp.Clones.Count > 0)
        {
            var clone = comp.Clones[^1];

            if (!Exists(clone) || TerminatingOrDeleted(clone))
            {
                comp.Clones.RemoveAt(comp.Clones.Count - 1);
                continue;
            }

            args.Handled = true;

            var cloneCoords = Transform(clone).Coordinates;
            _transform.SetCoordinates(uid, cloneCoords);

            HealFlat(uid, TeleportHealAmount);

            if (TryComp<CloneSummonComponent>(clone, out var cloneSummonComp))
                DespawnClone(clone, cloneSummonComp);
            else
                QueueDel(clone);

            return;
        }
    }

    private void HealFlat(EntityUid uid, FixedPoint2 amount)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable) || damageable.TotalDamage <= FixedPoint2.Zero)
            return;

        var healAmount = FixedPoint2.Min(amount, damageable.TotalDamage);
        var scale = healAmount / damageable.TotalDamage;

        var heal = new DamageSpecifier();
        foreach (var (type, value) in damageable.Damage.DamageDict)
        {
            if (value <= FixedPoint2.Zero)
                continue;

            heal.DamageDict[type] = -(value * scale);
        }

        _damageable.TryChangeDamage(
            uid,
            heal,
            ignoreResistances: true,
            interruptsDoAfters: false,
            origin: null,
            canMiss: false);

        var result = _damageable.TryChangeDamage(
    uid, heal,
    ignoreResistances: true,
    interruptsDoAfters: false,
    origin: null,
    canMiss: false);

        Log.Debug($"HealFlat result for {uid}: {result}");
    }

    private void OnCloneMobStateChanged(EntityUid uid, CloneSummonComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            DespawnClone(uid, comp);
    }

    private void OnCloneAttackAttempt(EntityUid uid, CloneSummonComponent comp, AttackAttemptEvent args)
    {
        if (args.Cancelled || comp.Summoner == null || args.Target != comp.Summoner)
            return;

        args.Cancel();
    }

    private void DespawnClone(EntityUid uid, CloneSummonComponent comp)
    {
        if (comp.Despawning || TerminatingOrDeleted(uid))
            return;

        comp.Despawning = true;

        var coords = Transform(uid).Coordinates;

        DropNonOriginalItems(uid, comp);

        if (comp.DespawnEffect != null)
            Spawn(comp.DespawnEffect, coords);

        foreach (var item in comp.OriginalItems)
        {
            if (Exists(item))
                QueueDel(item);
        }

        CleanupSummonerLinks(uid, comp);

        QueueDel(uid);
    }

    private void DropNonOriginalItems(EntityUid uid, CloneSummonComponent comp)
    {
        if (HasComp<HandsComponent>(uid))
        {
            foreach (var held in _hands.EnumerateHeld(uid).ToList())
            {
                if (comp.OriginalItems.Contains(held))
                    continue;

                _hands.TryDrop(uid, held, checkActionBlocker: false);
            }
        }

        if (TryComp<InventoryComponent>(uid, out var inventory)
            && _inventory.TryGetContainerSlotEnumerator((uid, inventory), out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (!_inventory.TryGetSlotEntity(uid, slot.ID, out var itemUid))
                    continue;

                if (comp.OriginalItems.Contains(itemUid.Value))
                    continue;

                _inventory.TryUnequip(uid, slot.ID, force: true, silent: true);
            }
        }
    }

    private void CleanupSummonerLinks(EntityUid clone, CloneSummonComponent comp)
    {
        if (comp.Summoner is not { } summoner || TerminatingOrDeleted(summoner))
            return;

        if (!TryComp<CloneSummonerComponent>(summoner, out var summonerComp))
            return;

        summonerComp.Clones.Remove(clone);

        if (summonerComp.Clones.Count > 0)
            return;

        if (summonerComp.TeleportActionEntity != null)
            _actions.RemoveAction(summoner, summonerComp.TeleportActionEntity);

        RemComp<CloneSummonerComponent>(summoner);
    }

    private void OnCloneComponentShutdown(EntityUid uid, CloneSummonComponent comp, ComponentShutdown args)
    {
        if (comp.Despawning)
            return;

        CleanupSummonerLinks(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CloneSummonComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Despawning)
                continue;

            comp.TimeToLive -= frameTime;
            if (comp.TimeToLive <= 0f)
                DespawnClone(uid, comp);
        }
    }
}
