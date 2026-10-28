using Content.Shared.Power.Components;

namespace Content.Server._EinsteinEngines.Power;

public sealed partial class BatteryDrinkerSystem
{
    private float ClampDrinkAmount(
        Entity<BatteryComponent> source,
        Entity<BatteryComponent> drinker,
        ref float amountToDrink)
    {
        var sourceCharge = _battery.GetCharge(source.AsNullable());
        var drinkerCharge = _battery.GetCharge(drinker.AsNullable());
        amountToDrink = MathF.Min(amountToDrink, sourceCharge);
        amountToDrink = MathF.Min(amountToDrink, drinker.Comp.MaxCharge - drinkerCharge);
        return sourceCharge;
    }

    private void TransferDrinkCharge(EntityUid source, EntityUid drinker, float amountToDrink, float sourceCharge)
    {
        if (_battery.TryUseCharge(source, amountToDrink))
        {
            _battery.ChangeCharge(drinker, amountToDrink);
            return;
        }

        _battery.ChangeCharge(drinker, sourceCharge);
        _battery.SetCharge(source, 0);
    }
}
