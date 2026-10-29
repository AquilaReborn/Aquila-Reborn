using Content.Shared._Aquila.Power;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;

namespace Content.Goobstation.Shared.Augments;

public abstract partial class SharedAugmentPowerCellSystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    private void InitializeAquila()
    {
        SubscribeLocalEvent<AugmentPowerCellSlotComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<HasAugmentPowerCellSlotComponent, FindDrinkerBatteryEvent>(OnFindDrinkerBattery);
        SubscribeLocalEvent<AugmentPowerCellSlotComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        SubscribeLocalEvent<AugmentStationRechargeComponent, FindChargerBatteryEvent>(OnFindChargerBattery);
    }

    private void RefreshAugmentChargeRate(EntityUid slot)
    {
        if (_powerCell.TryGetBatteryFromSlot(slot, out var battery))
            _battery.RefreshChargeRate(battery.Value.AsNullable());
    }

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
}
