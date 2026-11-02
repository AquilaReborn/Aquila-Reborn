using Content.Server._Aquila.Cyberware.Components;
using Content.Shared._Aquila.Cyberware.EntityEffects;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Storage;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Server.Administration.Logs;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Cyberware;

/// <summary>
/// Приступы ярости киберпсихопата: на время приступа телом управляет ИИ нападения.
/// </summary>
public sealed class CyberpsychosisRampageSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberpsychosisRampageComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CyberpsychosisRampageComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now >= comp.EndTime)
                RemCompDeferred<CyberpsychosisRampageComponent>(uid);
        }
    }

    /// <summary>
    /// Начинает приступ ярости: больной выхватывает оружие, а телом на время начинает управлять ИИ нападения.
    /// </summary>
    public bool TryStartRampage(EntityUid uid, CyberpsychosisRampage settings, float scale = 1f)
    {
        if (HasComp<CyberpsychosisRampageComponent>(uid))
            return false;

        var comp = AddComp<CyberpsychosisRampageComponent>(uid);
        comp.EndTime = _timing.CurTime + settings.Duration * scale;

        TryDrawWeapon(uid);

        if (TryComp<NpcFactionMemberComponent>(uid, out var member))
        {
            comp.SavedFactions = new(member.Factions);
            _faction.ClearFactions((uid, member), false);
            _faction.AddFaction((uid, member), settings.Faction);
        }

        var toAdd = new ComponentRegistry();
        foreach (var (name, entry) in settings.Components)
        {
            if (HasComp(uid, entry.Component.GetType()))
                continue;

            toAdd.Add(name, entry);
            comp.AddedComponents.Add(name);
        }

        EntityManager.AddComponents(uid, toAdd);

        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(uid):player} lost control to a cyberpsychosis rampage");
        return true;
    }

    private void OnShutdown(Entity<CyberpsychosisRampageComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        foreach (var name in ent.Comp.AddedComponents)
        {
            if (_factory.TryGetRegistration(name, out var registration))
                RemComp(ent.Owner, registration.Type);
        }

        if (TryComp<NpcFactionMemberComponent>(ent, out var member))
        {
            _faction.ClearFactions((ent.Owner, member), false);
            _faction.AddFactions((ent.Owner, member), ent.Comp.SavedFactions);
        }
    }

    private void TryDrawWeapon(EntityUid uid)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return;

        var held = _hands.GetActiveItem((uid, hands));
        if (held != null && IsWeapon(held.Value))
            return;

        if (FindWeapon(uid) is not { } weapon)
            return;

        if (!_hands.TryGetEmptyHand((uid, hands), out var emptyHand))
            return;

        _hands.TrySetActiveHand((uid, hands), emptyHand);
        _hands.TryPickup(uid, weapon, emptyHand, checkActionBlocker: false, handsComp: hands);
    }

    private EntityUid? FindWeapon(EntityUid uid)
    {
        EntityUid? melee = null;

        foreach (var held in _hands.EnumerateHeld(uid))
        {
            if (HasComp<GunComponent>(held))
                return held;

            if (melee == null && IsWeapon(held))
                melee = held;
        }

        if (!_inventory.TryGetContainerSlotEnumerator(uid, out var slots))
            return melee;

        while (slots.NextItem(out var item))
        {
            if (HasComp<GunComponent>(item))
                return item;

            if (melee == null && IsWeapon(item))
                melee = item;

            if (!TryComp<StorageComponent>(item, out var storage))
                continue;

            foreach (var stored in storage.Container.ContainedEntities)
            {
                if (HasComp<GunComponent>(stored))
                    return stored;

                if (melee == null && IsWeapon(stored))
                    melee = stored;
            }
        }

        return melee;
    }

    private bool IsWeapon(EntityUid item)
    {
        return HasComp<GunComponent>(item) || HasComp<MeleeWeaponComponent>(item);
    }
}
