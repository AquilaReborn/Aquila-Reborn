namespace Content.Shared._Aquila.Economy.Types;

/// <summary>
/// Уровни защиты банковского счёта: от доступа по одной карте до PIN во всех операциях.
/// </summary>
public static class BankSecurityLevels
{
    /// <summary>
    /// Доступ по карте: PIN не нужен.
    /// </summary>
    public const int Open = 0;

    /// <summary>
    /// Карта и PIN при покупках в автоматах и платёжном терминале.
    /// </summary>
    public const int Purchase = 1;

    /// <summary>
    /// Карта и PIN во всех операциях, включая банкомат и КПК.
    /// </summary>
    public const int Full = 2;

    private static readonly LocId LowName = "atm-security-level-low";
    private static readonly LocId MediumName = "atm-security-level-medium";
    private static readonly LocId HighName = "atm-security-level-high";

    /// <summary>
    /// Локализованное название уровня защиты для показа игроку.
    /// </summary>
    public static LocId GetName(int level)
    {
        return level switch
        {
            Purchase => MediumName,
            Full => HighName,
            _ => LowName,
        };
    }
}
