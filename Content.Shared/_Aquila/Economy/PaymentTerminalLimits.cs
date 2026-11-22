namespace Content.Shared._Aquila.Economy;

/// <summary>
/// Общие пределы платёжного терминала.
/// </summary>
public static class PaymentTerminalLimits
{
    /// <summary>
    /// Предельная длина назначения счёта: более длинное назначение обрезается.
    /// </summary>
    public const int MaxPurposeLength = 48;
}
