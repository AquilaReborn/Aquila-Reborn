using Content.Goobstation.Common.Augments;
using Content.Shared.Body.Systems;
using Content.Shared.PowerCell;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.PowerCell.Components;
using Content.Shared._Aquila.Power; // Aquila Change
using Content.Shared.Power; // Aquila Change
using Content.Shared.Power.EntitySystems; // Aquila Change

namespace Content.Goobstation.Shared.Augments;

public abstract class SharedAugmentPowerCellSystem : CommonAugmentPowerCellSystem
{
    [Dependency] protected readonly AugmentSystem Augment = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] protected readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!; // Aquila Change

    public override void Initialize()
    {
        base.Initialize();

        _drawQuery = GetEntityQuery<PowerCellDrawComponent>();

        SubscribeLocalEvent<AugmentPowerCellSlotComponent, OrganEnableChangedEvent>(OnEnableChanged);
        SubscribeLocalEvent<AugmentPowerCellSlotComponent, PowerCellSlotEmptyEvent>(OnCellEmpty);
        // Aquila Change start
        SubscribeLocalEvent<AugmentPowerCellSlotComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<HasAugmentPowerCellSlotComponent, FindDrinkerBatteryEvent>(OnFindDrinkerBattery);
        SubscribeLocalEvent<AugmentPowerCellSlotComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        SubscribeLocalEvent<AugmentStationRechargeComponent, FindChargerBatteryEvent>(OnFindChargerBattery);
        // Aquila Change end
    }

    private EntityQuery<PowerCellDrawComponent> _drawQuery;

    private void OnEnableChanged(Entity<AugmentPowerCellSlotComponent> ent, ref OrganEnableChangedEvent args)
    {
        if (!_drawQuery.TryComp(ent, out var draw))
            return;

        UpdateDrawRate((ent, draw));

        _powerCell.SetDrawEnabled(ent.Owner, args.Enabled);
        if (Augment.GetBody(ent) is not {} body)
            return;

        if (args.Enabled && HasPower(ent.Owner)) // Aquila Change
        {
            var ev = new AugmentGainedPowerEvent(body);
            Augment.RelayEvent(body, ref ev);
        }
        else
        {
            var ev = new AugmentLostPowerEvent(body);
            Augment.RelayEvent(body, ref ev);
        }
    }

    private void OnCellEmpty(Entity<AugmentPowerCellSlotComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (Augment.GetBody(ent) is not {} body)
            return;

        var ev = new AugmentLostPowerEvent(body);
        Augment.RelayEvent(body, ref ev);

        // stop drawing if it loses power
        UpdateDrawRate(ent.Owner);
    }

    public float GetBodyDraw(EntityUid body)
    {
        var ev = new GetAugmentsPowerDrawEvent(body);
        Augment.RelayEvent(body, ref ev);
        return ev.TotalDraw;
    }

    /// <summary>
    /// Update the draw rate for a power cell slot augment.
    /// </summary>
    public void UpdateDrawRate(Entity<PowerCellDrawComponent?> ent)
    {
        if (!_drawQuery.Resolve(ent, ref ent.Comp))
            return;

        var rate = Augment.GetBody(ent) is {} body
            ? GetBodyDraw(body)
            : 0f;
        if (ent.Comp.DrawRate == rate)
            return;

        ent.Comp.DrawRate = rate;
        Dirty(ent, ent.Comp);

        // Aquila Change start
        if (_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery))
            _battery.RefreshChargeRate(battery.Value.AsNullable());
        // Aquila Change end
    }

    /// <summary>
    /// Get a body's power cell slot augment, or null if it has none.
    /// </summary>
    public Entity<AugmentPowerCellSlotComponent>? GetBodyAugment(EntityUid body)
    {
        foreach (var augment in _body.GetBodyOrganEntityComps<AugmentPowerCellSlotComponent>(body))
        {
            return (augment.Owner, augment.Comp1);
        }

        return null;
    }

    // Aquila Change start
    /// <summary>
    /// Есть ли в слоте импланта батарея с ненулевым зарядом.
    /// </summary>
    public bool HasPower(EntityUid slot)
    {
        return _powerCell.TryGetBatteryFromSlot(slot, out var battery)
            && _battery.GetCharge(battery.Value.AsNullable()) > 0f;
    }

    private void OnRefreshChargeRate(Entity<AugmentPowerCellSlotComponent> ent, ref RefreshChargeRateEvent args)
    {
        RelayToStationRechargeBody(ent, ref args);
    }

    private void OnBatteryStateChanged(Entity<AugmentPowerCellSlotComponent> ent, ref BatteryStateChangedEvent args)
    {
        RelayToStationRechargeBody(ent, ref args);
    }

    private void RelayToStationRechargeBody<T>(EntityUid slot, ref T args) where T : notnull
    {
        if (Augment.GetBody(slot) is { } body && HasComp<AugmentStationRechargeComponent>(body))
            RaiseLocalEvent(body, ref args);
    }

    private void OnFindDrinkerBattery(Entity<HasAugmentPowerCellSlotComponent> ent, ref FindDrinkerBatteryEvent args)
    {
        if (GetBodyAugment(ent) is { } slot && _powerCell.TryGetBatteryFromSlot(slot.Owner, out var battery))
            args.Battery = battery.Value.Owner;
    }

    private void OnFindChargerBattery(Entity<AugmentStationRechargeComponent> ent, ref FindChargerBatteryEvent args)
    {
        if (GetBodyAugment(ent) is { } slot && _powerCell.TryGetBatteryFromSlot(slot.Owner, out var battery))
            args.Battery = battery;
    }
    // Aquila Change end
}
