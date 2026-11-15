namespace Content.Client.Cargo.UI;

public sealed partial class CargoPalletMenu
{
    /// <summary>
    /// Показывает, сколько станция выручит за товары с учётом насыщения рынка.
    /// </summary>
    public void SetPayout(int payout, int appraisal)
    {
        PayoutLabel.Text = appraisal > 0 && payout < appraisal
            ? Loc.GetString("cargo-pallet-menu-payout-saturated",
                ("amount", payout.ToString()),
                ("saturation", (int) MathF.Round(100f * (appraisal - payout) / appraisal)))
            : Loc.GetString("cargo-console-menu-points-amount", ("amount", payout.ToString()));
    }
}
