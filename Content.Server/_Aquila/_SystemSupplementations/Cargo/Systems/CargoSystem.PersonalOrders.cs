using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private bool CanAffordPersonalOrder(
        EntityUid console,
        CargoOrderConsoleComponent component,
        EntityUid actor,
        EntityUid station,
        CargoOrderData order,
        int cost)
    {
        if (order.PersonalAccount == null || _personalOrders.CanAfford(station, order, cost))
            return true;

        ConsolePopup(actor, Loc.GetString("cargo-console-personal-insufficient-funds", ("cost", cost)));
        PlayDenySound(console, component);
        return false;
    }

    private void PayForOrder(Entity<StationBankAccountComponent> station, CargoOrderData order, int cost)
    {
        if (order.PersonalAccount != null)
            _personalOrders.Pay(station.Owner, order, cost);
        else
            UpdateBankAccount(station.AsNullable(), -cost, order.Account);
    }

    private bool TryGetPersonalAccount(
        EntityUid console,
        CargoOrderConsoleComponent component,
        EntityUid player,
        EntityUid station,
        bool personal,
        out int? personalAccount)
    {
        personalAccount = personal ? _personalOrders.GetPayerAccount(station, player) : null;
        if (!personal || personalAccount != null)
            return true;

        ConsolePopup(player, Loc.GetString("cargo-console-personal-no-account"));
        PlayDenySound(console, component);
        return false;
    }
}
