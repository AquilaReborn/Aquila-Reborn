using System.Linq;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private void SetPalletSaleState(
        EntityUid console,
        HashSet<(EntityUid, OverrideSellComponent?, double)> goods,
        double totalAmount,
        int sellCount)
    {
        var payout = _station.GetOwningStation(console) is { } station
            ? _market.Appraise(station, goods.Select(good => (good.Item1, good.Item3)))
            : totalAmount;
        _uiSystem.SetUiState(console,
            CargoPalletConsoleUiKey.Sale,
            new CargoPalletConsoleInterfaceState((int) totalAmount, sellCount, true) { Payout = (int) payout });
    }
}
