using Content.Shared._Aquila.Actions;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Emp;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Shared._Aquila.Cyberware;

/// <summary>
/// Не даёт пользоваться имплантами, отключёнными ЭМИ или заглушёнными ошейником-блокиратором.
/// </summary>
public sealed class CyberwareBlockSystem : EntitySystem
{
    private static readonly LocId EmpDisabledMessage = "cyberware-action-disabled";
    private static readonly LocId BlockedMessage = "cyberware-action-blocked";

    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareComponent, ActionProviderAttemptEvent>(OnCyberwareActionAttempt);

        SubscribeLocalEvent<CyberwareBlockerComponent, GotEquippedEvent>(OnBlockerEquipped);
        SubscribeLocalEvent<CyberwareBlockerComponent, GotUnequippedEvent>(OnBlockerUnequipped);
    }

    /// <summary>
    /// Возвращает тело, в котором установлен имплант, орган или часть тела.
    /// </summary>
    public EntityUid? GetHost(EntityUid uid)
    {
        if (TryComp<OrganComponent>(uid, out var organ))
            return organ.Body;

        if (TryComp<BodyPartComponent>(uid, out var part))
            return part.Body;

        return CompOrNull<SubdermalImplantComponent>(uid)?.ImplantedEntity;
    }

    /// <summary>
    /// Заглушены ли импланты существа блокиратором.
    /// </summary>
    public bool IsBlocked(EntityUid uid)
    {
        return HasComp<CyberwareBlockedComponent>(uid);
    }

    private void OnCyberwareActionAttempt(Entity<CyberwareComponent> ent, ref ActionProviderAttemptEvent args)
    {
        TryBlockAction(ent, ref args);
    }

    /// <summary>
    /// Запрещает действие, если его выдал отключённый ЭМИ имплант или имплант заглушённого существа.
    /// </summary>
    public void TryBlockAction(EntityUid provider, ref ActionProviderAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        LocId message;
        if (HasComp<EmpDisabledComponent>(provider))
            message = EmpDisabledMessage;
        else if (GetHost(provider) is { } host && IsBlocked(host))
            message = BlockedMessage;
        else
            return;

        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString(message), args.User, args.User);
    }

    private void OnBlockerEquipped(Entity<CyberwareBlockerComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & SlotFlags.NECK) == 0)
            return;

        var blocked = EnsureComp<CyberwareBlockedComponent>(args.Equipee);
        blocked.LoadMultiplier = ent.Comp.LoadMultiplier;
        blocked.Blocker = ent.Owner;
        Dirty(args.Equipee, blocked);
    }

    private void OnBlockerUnequipped(Entity<CyberwareBlockerComponent> ent, ref GotUnequippedEvent args)
    {
        if (!TryComp<CyberwareBlockedComponent>(args.Equipee, out var blocked)
            || blocked.Blocker != ent.Owner)
            return;

        RemComp<CyberwareBlockedComponent>(args.Equipee);
    }
}
