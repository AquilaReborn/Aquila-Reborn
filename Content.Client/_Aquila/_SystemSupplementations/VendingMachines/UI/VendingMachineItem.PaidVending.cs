namespace Content.Client.VendingMachines.UI;

public sealed partial class VendingMachineItem
{
    /// <summary>
    /// Показывает цену товара справа от названия.
    /// </summary>
    public void SetPrice(string text)
    {
        PriceLabel.Text = text;
        PriceLabel.Visible = true;
    }
}
