namespace Content.Shared._Aquila.Economy;

/// <summary>
/// Данные последней успешной оплаты через платёжный терминал: из них печатается чек.
/// </summary>
/// <param name="Amount">Сумма оплаты в кредитах.</param>
/// <param name="PayerName">Имя владельца счёта, с которого списали кредиты.</param>
/// <param name="Purpose">Назначение оплаты на момент платежа.</param>
/// <param name="Time">Время оплаты от начала раунда.</param>
public sealed record PaymentTerminalReceipt(int Amount, string PayerName, string Purpose, TimeSpan Time);
