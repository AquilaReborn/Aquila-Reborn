namespace Content.Client.Salvage.UI;

public sealed partial class JobEntry
{
    private static readonly LocId TakenByYou = "bounty-console-contract-taken-you";
    private static readonly LocId TakenByOther = "bounty-console-contract-taken-other";

    public Action? OnClaimButtonPressed;

    /// <summary>
    /// Подписывает кнопку взятия личной квоты и показывает, свободна работа или уже кем-то взята.
    /// </summary>
    public void AquilaSetupClaim(bool taken, bool isMine)
    {
        ClaimButton.OnPressed += _ => OnClaimButtonPressed?.Invoke();

        ClaimButton.Disabled = taken && !isMine;
        PrintButton.Disabled = taken && !isMine;
        StatusLabel.Text = taken
            ? Loc.GetString(isMine ? TakenByYou : TakenByOther)
            : string.Empty;
    }
}
