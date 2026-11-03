using System.Numerics;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Funkystation.VendingMachines.UI;

public sealed partial class VendingMachineGridSlot
{
    private const float PriceMargin = 2f;
    private const float PriceHeight = 16f;

    private Label? _priceLabel;

    /// <summary>
    /// Показывает цену товара в углу слота. Null убирает подпись.
    /// </summary>
    public void SetPrice(string? price)
    {
        if (price == null)
        {
            if (_priceLabel != null)
                _priceLabel.Visible = false;

            return;
        }

        if (_priceLabel == null)
        {
            _priceLabel = new Label { StyleClasses = { StyleNano.StyleClassLabelSubText } };
            SlotLayout.AddChild(_priceLabel);
            LayoutContainer.SetPosition(_priceLabel, new Vector2(PriceMargin, SlotSize - PriceHeight - PriceMargin));
        }

        _priceLabel.Text = price;
        _priceLabel.Visible = true;
    }
}
