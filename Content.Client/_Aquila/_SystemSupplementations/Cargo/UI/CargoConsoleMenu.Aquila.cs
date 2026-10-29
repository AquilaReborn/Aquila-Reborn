using Content.Shared._Aquila.CCVars;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;

namespace Content.Client.Cargo.UI;

public sealed partial class CargoConsoleMenu
{
    private void UpdateMarketInflationVisibility()
    {
        var economy = _cfg.GetCVar(AquilaCVars.Economy);
        MarketInflationTitle.Visible = economy;
        MarketInflationDivider.Visible = economy;
        MarketInflationLabel.Visible = economy;
    }

    private static void ApplyPersonalOrderName(CargoOrderRow row, CargoOrderData order, string requester)
    {
        if (order.PersonalAccount is not { } personalAccount)
            return;

        row.ProductName.Text = Loc.GetString("cargo-console-menu-order-row-personal",
            ("orderRequester", requester),
            ("number", personalAccount.ToString()));
    }

    private void AddWithdrawOption(ref int index)
    {
        if (_cfg.GetCVar(AquilaCVars.Economy))
            return;

        ActionOptions.AddItem(Loc.GetString("cargo-console-menu-account-action-option-withdraw"), index);
        index++;
    }

    /// <summary>
    /// Возвращает цену товара с учётом роста цен на рынке станции.
    /// </summary>
    public int GetProductCost(CargoProductPrototype product)
    {
        return _station is { } station
            ? product.Cost + _market.GetMarkup(station, product.Cost)
            : product.Cost;
    }
}
